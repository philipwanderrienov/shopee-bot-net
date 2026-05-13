using System;
using Microsoft.EntityFrameworkCore;
using shopeebotnet.Server.Models;

namespace shopeebotnet.Server.DbContext;

public class ShopeeAffiliateContext : Microsoft.EntityFrameworkCore.DbContext
{
    public ShopeeAffiliateContext(DbContextOptions<ShopeeAffiliateContext> options) : base(options)
    {
    }

    public DbSet<AppUserModel> AppUsers => Set<AppUserModel>();
    public DbSet<ProductModel> Products => Set<ProductModel>();
    public DbSet<AffiliateLinkModel> AffiliateLinks => Set<AffiliateLinkModel>();
    public DbSet<ClickModel> Clicks => Set<ClickModel>();
    public DbSet<ConversionModel> Conversions => Set<ConversionModel>();
    public DbSet<DailyRecommendationModel> DailyRecommendations => Set<DailyRecommendationModel>();
    public DbSet<ScoringSettingModel> ScoringSettings => Set<ScoringSettingModel>();
    public DbSet<AffiliateCredentialModel> AffiliateCredentials => Set<AffiliateCredentialModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUserModel>(entity =>
        {
            entity.ToTable("app_users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
            entity.Property(x => x.Email).HasColumnName("email").IsRequired();
            entity.Property(x => x.Role).HasColumnName("role").HasConversion<string>();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<ProductModel>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.ProductIdOnPlatform).HasColumnName("product_id_on_platform");
            entity.Property(x => x.CommissionRate).HasColumnName("commission_rate");
            entity.Property(x => x.Category).HasColumnName("category");
            entity.Property(x => x.ReviewCount).HasColumnName("review_count");
            entity.Property(x => x.Rating).HasColumnName("rating");
            entity.Property(x => x.SalesVolume).HasColumnName("sales_volume");
            entity.Property(x => x.ImageUrl).HasColumnName("image_url");
            entity.Property(x => x.DataSource).HasColumnName("data_source");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<AffiliateLinkModel>(entity =>
        {
            entity.ToTable("affiliate_links");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.ProductId).HasColumnName("product_id");
            entity.Property(x => x.OriginalUrl).HasColumnName("original_url");
            entity.Property(x => x.ShortCode).HasColumnName("short_code");
            entity.Property(x => x.ShortUrl).HasColumnName("short_url");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<ClickModel>(entity =>
        {
            entity.ToTable("clicks");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.LinkId).HasColumnName("link_id");
            entity.Property(x => x.ProductId).HasColumnName("product_id");
            entity.Property(x => x.Timestamp).HasColumnName("timestamp");
            entity.Property(x => x.Ip).HasColumnName("ip");
            entity.Property(x => x.UserAgent).HasColumnName("user_agent");
            entity.Property(x => x.TrafficSource).HasColumnName("traffic_source");
            entity.Property(x => x.Converted).HasColumnName("converted");
        });

        modelBuilder.Entity<ConversionModel>(entity =>
        {
            entity.ToTable("conversions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.ClickId).HasColumnName("click_id");
            entity.Property(x => x.OrderId).HasColumnName("order_id");
            entity.Property(x => x.Commission).HasColumnName("commission");
            entity.Property(x => x.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(x => x.RecordedAt).HasColumnName("recorded_at");
        });

        modelBuilder.Entity<DailyRecommendationModel>(entity =>
        {
            entity.ToTable("daily_recommendations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.ProductId).HasColumnName("product_id");
            entity.Property(x => x.Score).HasColumnName("score");
            entity.Property(x => x.RecommendationDate).HasColumnName("recommendation_date");
            entity.Property(x => x.WeightBreakdown).HasColumnName("weight_breakdown");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<ScoringSettingModel>(entity =>
        {
            entity.ToTable("scoring_settings");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Weights).HasColumnName("weights");
            entity.Property(x => x.ActiveFrom).HasColumnName("active_from");
            entity.Property(x => x.ActiveTo).HasColumnName("active_to");
            entity.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<AffiliateCredentialModel>(entity =>
        {
            entity.ToTable("affiliate_credentials");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.OwnerUserId).HasColumnName("owner_user_id");
            entity.Property(x => x.NetworkName).HasColumnName("network_name");
            entity.Property(x => x.ApiKeyEncrypted).HasColumnName("api_key_encrypted");
            entity.Property(x => x.ApiSecretEncrypted).HasColumnName("api_secret_encrypted");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });
    }
}
