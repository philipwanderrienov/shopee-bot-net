using Microsoft.EntityFrameworkCore;
using jitu_dashboard.Server.DbContext;
using jitu_dashboard.Server.EFRepository;
using jitu_dashboard.Server.Models;
using jitu_dashboard.Server.Repository;
using jitu_dashboard.Server.Repository.Payment;
using jitu_dashboard.Server.Repository.PaymentHistory;
using jitu_dashboard.Server.Services.Payment;
using jitu_dashboard.Server.Services.PaymentHistory;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

var sqlServerConnectionString = builder.Configuration.GetConnectionString("SqlServerContext");
if (string.IsNullOrWhiteSpace(sqlServerConnectionString))
{
    throw new InvalidOperationException("ConnectionStrings:SqlServerContext is missing or empty in appsettings.json.");
}

builder.Services.AddDbContext<JituDashboardContext>(options =>
    options.UseSqlServer(sqlServerConnectionString));

builder.Services.AddSwaggerGen();

// Register repositories and services
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddScoped<IPaymentHistoryRepository, PaymentHistoryRepository>();
builder.Services.AddScoped<IPaymentHistoryService, PaymentHistoryService>();

var app = builder.Build();

app.UseStaticFiles();
app.UseDefaultFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
