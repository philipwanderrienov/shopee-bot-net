using jitu_dashboard.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace jitu_dashboard.Server.DbContext;

public class JituDashboardContext : Microsoft.EntityFrameworkCore.DbContext
{
    public JituDashboardContext(DbContextOptions<JituDashboardContext> options) : base(options) { }

    public DbSet<PaymentModel> PaymentModel { get; set; }
    public DbSet<PaymentHistoryModel> PaymentHistoryModel { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentModel>()
            .ToTable("Payment", "dbo")
            .HasKey(o => new { o.Trn });

        modelBuilder.Entity<PaymentHistoryModel>()
            .ToTable("PaymentHistory", "dbo")
            .HasKey(o => new { o.Trn });
    }
}
