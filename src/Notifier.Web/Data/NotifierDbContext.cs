using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Notifier.Core.Domain;

namespace Notifier.Web.Data;

public sealed class NotifierDbContext(DbContextOptions<NotifierDbContext> options) : DbContext(options)
{
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<ArticleTopic> ArticleTopics => Set<ArticleTopic>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<AlertTopic> AlertTopics => Set<AlertTopic>();
    public DbSet<Destination> Destinations => Set<Destination>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<FeedStatus> FeedStatuses => Set<FeedStatus>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        // SQLite returns DateTime with Kind=Unspecified.
        builder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        builder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
        builder.Properties<Topic>().HaveConversion<string>();
        builder.Properties<Tier>().HaveConversion<string>();
        builder.Properties<DeliveryKind>().HaveConversion<string>();
        builder.Properties<DeliveryStatus>().HaveConversion<string>();
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Article>(e =>
        {
            e.HasKey(a => a.Key);
            e.HasMany(a => a.Topics).WithOne().HasForeignKey(t => t.ArticleKey).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(a => a.LastSeenAtUtc);
        });
        b.Entity<ArticleTopic>().HasKey(t => new { t.ArticleKey, t.Topic });

        b.Entity<Alert>(e =>
        {
            e.HasIndex(a => a.OwnerUsername);
            e.HasMany(a => a.Topics).WithOne().HasForeignKey(t => t.AlertId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(a => a.Destinations).WithOne().HasForeignKey(d => d.AlertId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<AlertTopic>().HasKey(t => new { t.AlertId, t.Topic });

        b.Entity<Delivery>(e =>
        {
            e.HasIndex(d => new { d.ArticleKey, d.AlertId, d.DestinationId }).IsUnique();
            e.HasIndex(d => d.Status);
            e.HasIndex(d => d.CreatedAtUtc);
            // Restrict: delivery history must outlive alerts and articles.
            e.HasOne<Alert>().WithMany().HasForeignKey(d => d.AlertId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Destination>().WithMany().HasForeignKey(d => d.DestinationId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Article>().WithMany().HasForeignKey(d => d.ArticleKey).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<FeedStatus>().HasKey(f => f.Topic);
    }

    // Fail rather than convert: ToUniversalTime() treats Kind=Unspecified as local time.
    internal static DateTime EnsureUtc(DateTime v) => v.Kind == DateTimeKind.Utc
        ? v
        : throw new InvalidOperationException($"Non-UTC DateTime (Kind={v.Kind}) written to the database; use TimeProvider.GetUtcNow().UtcDateTime.");

    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        v => EnsureUtc(v),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    private sealed class NullableUtcDateTimeConverter() : ValueConverter<DateTime?, DateTime?>(
        v => v == null ? v : EnsureUtc(v.Value),
        v => v == null ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc));
}
