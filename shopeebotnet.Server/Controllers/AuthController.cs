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

        var user = new AppUserModel
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = AffiliateRoleModel.affiliate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();

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

        var user = await _db.AppUsers.FirstOrDefaultAsync(x => x.Email == normalizedEmail);
        if (user == null)
            return Unauthorized(new { message = "Invalid credentials." });

        var ok = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!ok)
            return Unauthorized(new { message = "Invalid credentials." });

        var token = GenerateAccessToken(user);
        var minutes = _configuration.GetValue<int>("Jwt:AccessTokenMinutes");

        return Ok(new AuthResponse(token, minutes));
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
            // Must match TokenValidationParameters NameClaimType="sub" and RoleClaimType="role"
            new Claim("sub", user.Id.ToString()),
            new Claim("role", user.Role.ToString())
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
