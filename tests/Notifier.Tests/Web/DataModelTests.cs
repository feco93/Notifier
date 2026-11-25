using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Notifier.Core.Domain;
using Notifier.Web.Data;

namespace Notifier.Tests.Web;

public sealed class DataModelTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc);
    private readonly SqliteConnection _conn = new("Data Source=:memory:");

    public DataModelTests() => _conn.Open();
    public void Dispose() => _conn.Dispose();

    private NotifierDbContext NewContext()
    {
        var db = new NotifierDbContext(new DbContextOptionsBuilder<NotifierDbContext>().UseSqlite(_conn).Options);
        db.Database.EnsureCreated();
        return db;
    }

    private (Alert alert, Destination dest, Article article) Seed(NotifierDbContext db)
    {
        var article = new Article { Key = "c123", Title = "T", Url = "https://www.bbc.co.uk/news/articles/c123",
            FirstSeenAtUtc = Now, LastSeenAtUtc = Now, PublishedAtUtc = Now };
        var alert = new Alert { OwnerUsername = "u", Name = "A", Tier = Tier.All, CreatedAtUtc = Now, ActiveSinceUtc = Now,
            Topics = [new AlertTopic { Topic = Topic.World }],
            Destinations = [new Destination { ChannelKey = "email", ConfigJson = "{}" }] };
        db.AddRange(article, alert);
        db.SaveChanges();
        return (alert, alert.Destinations[0], article);
    }

    private static Delivery NewDelivery(Alert a, Destination d, string? articleKey, DeliveryKind kind = DeliveryKind.Real) =>
        new() { AlertId = a.Id, DestinationId = d.Id, ArticleKey = articleKey, Kind = kind,
                Status = DeliveryStatus.Pending, CreatedAtUtc = Now };

    [Fact]
    public void Same_article_alert_destination_cannot_be_delivered_twice()
    {
        using var db = NewContext();
        var (alert, dest, article) = Seed(db);
        db.Deliveries.Add(NewDelivery(alert, dest, article.Key));
        db.SaveChanges();

        db.Deliveries.Add(NewDelivery(alert, dest, article.Key));
        var ex = Assert.Throws<DbUpdateException>(() => db.SaveChanges());
        Assert.Contains("UNIQUE", ex.InnerException!.Message);
    }

    [Fact]
    public void Repeated_test_sends_are_allowed_because_null_keys_are_distinct()
    {
        using var db = NewContext();
        var (alert, dest, _) = Seed(db);
        db.Deliveries.Add(NewDelivery(alert, dest, null, DeliveryKind.Test));
        db.Deliveries.Add(NewDelivery(alert, dest, null, DeliveryKind.Test));
        db.SaveChanges();
        Assert.Equal(2, db.Deliveries.Count(d => d.Kind == DeliveryKind.Test));
    }

    [Fact]
    public void DateTimes_come_back_as_utc()
    {
        using (var db = NewContext()) Seed(db);
        using var fresh = NewContext();
        var a = fresh.Articles.Single();
        Assert.Equal(DateTimeKind.Utc, a.FirstSeenAtUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, a.PublishedAtUtc!.Value.Kind);
        Assert.Equal(Now, a.FirstSeenAtUtc);
    }

    [Fact]
    public void Writing_a_non_utc_datetime_fails_loudly()
    {
        using var db = NewContext();
        db.Articles.Add(new Article { Key = "x", Title = "T", Url = "u",
            FirstSeenAtUtc = new DateTime(2026, 11, 24, 12, 0, 0, DateTimeKind.Unspecified), LastSeenAtUtc = Now });
        var ex = Assert.Throws<DbUpdateException>(() => db.SaveChanges());
        Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Contains("Non-UTC", ex.InnerException.Message);
    }

    [Fact]
    public void Hard_deleting_an_alert_with_history_is_blocked()
    {
        int alertId;
        using (var db = NewContext())
        {
            var (alert, dest, article) = Seed(db);
            db.Deliveries.Add(NewDelivery(alert, dest, article.Key));
            db.SaveChanges();
            alertId = alert.Id;
        }

        // Fresh context, so the delete reaches SQLite instead of being stopped by the change tracker.
        using var fresh = NewContext();
        fresh.Alerts.Remove(fresh.Alerts.Single(a => a.Id == alertId));
        var ex = Assert.Throws<DbUpdateException>(() => fresh.SaveChanges());
        Assert.Contains("FOREIGN KEY", ex.InnerException!.Message);
    }
}
