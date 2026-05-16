using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using shopeebotnet.Server.DbContext;
using shopeebotnet.Server.Models;

namespace shopeebotnet.Server.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ShopeeAffiliateContext _db;
    private readonly IConfiguration _configuration;

    public AuthController(ShopeeAffiliateContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public record RegisterRequest(string Email, string Password);
    public record LoginRequest(string Email, string Password);

    public record AuthResponse(string AccessToken, int AccessTokenMinutes);

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required." });

        if (request.Password.Length < 8)
            return BadRequest(new { message = "Password must be at least 8 characters." });

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var existing = await _db.AppUsers.FirstOrDefaultAsync(x => x.Email == normalizedEmail);
        if (existing != null)
            return Conflict(new { message = "Email already exists." });

        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var roleText = AffiliateRoleModel.affiliate.ToString();

        // Avoid EF/Npgsql enum binding issues by inserting with an explicit cast.
        // app_users.role is a PostgreSQL enum: affiliate_role
        await _db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO app_users (id, email, password_hash, role, created_at, updated_at)
            VALUES ({id}, {normalizedEmail}, {passwordHash}, {roleText}::text::affiliate_role, {now}, {now});
        ");

        var user = new AppUserModel
        {
            Id = id,
            Email = normalizedEmail,
            PasswordHash = passwordHash,
            Role = AffiliateRoleModel.affiliate,
            CreatedAt = now,
            UpdatedAt = now
        };

        var token = GenerateAccessToken(user);
        var minutes = _configuration.GetValue<int>("Jwt:AccessTokenMinutes");

        return Ok(new AuthResponse(token, minutes));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required." });

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // Read role as text to avoid Npgsql unmapped enum issues.
        var row = await GetUserForLoginAsync(normalizedEmail);
        if (row == null)
            return Unauthorized(new { message = "Invalid credentials." });

        var ok = BCrypt.Net.BCrypt.Verify(request.Password, row.PasswordHash);
        if (!ok)
            return Unauthorized(new { message = "Invalid credentials." });

        var role = ParseRole(row.RoleText);

        var user = new AppUserModel
        {
            Id = row.Id,
            Email = row.Email,
            PasswordHash = row.PasswordHash,
            Role = role
        };

        var token = GenerateAccessToken(user);
        var minutes = _configuration.GetValue<int>("Jwt:AccessTokenMinutes");

        return Ok(new AuthResponse(token, minutes));
    }

    private sealed record UserLoginRow(Guid Id, string Email, string PasswordHash, string RoleText);

    private async Task<UserLoginRow?> GetUserForLoginAsync(string normalizedEmail)
    {
        await using var conn = _db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, email, password_hash, role::text AS role_text
            FROM app_users
            WHERE email = @email
            LIMIT 1;";

        var emailParam = cmd.CreateParameter();
        emailParam.ParameterName = "@email";
        emailParam.Value = normalizedEmail;
        cmd.Parameters.Add(emailParam);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        var id = reader.GetGuid(0);
        var email = reader.GetString(1);
        var passwordHash = reader.GetString(2);
        var roleText = reader.GetString(3);

        return new UserLoginRow(id, email, passwordHash, roleText);
    }

    private static AffiliateRoleModel ParseRole(string roleText)
    {
        var normalized = roleText?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "admin" => AffiliateRoleModel.admin,
            _ => AffiliateRoleModel.affiliate
        };
    }

    private string GenerateAccessToken(AppUserModel user)
    {
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer missing");
        var jwtAudience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience missing");
        var jwtSecret = _configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret missing");

        var minutes = _configuration.GetValue<int>("Jwt:AccessTokenMinutes");
        if (minutes <= 0) minutes = 60;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            // Must match TokenValidationParameters NameClaimType="sub"
            new Claim("sub", user.Id.ToString()),

            // Support both claim types:
            // - "role" (used by TokenValidationParameters.RoleClaimType="role")
            // - ClaimTypes.Role (used by IsInRole / Roles authorization)
            new Claim("role", user.Role.ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
