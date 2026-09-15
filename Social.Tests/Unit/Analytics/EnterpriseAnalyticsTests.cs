using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Social.API.Middlewares;
using Social.Core.Entities;
using Social.Infrastructure.Data;
using Social.Infrastructure.Services;
using Social.Infrastructure.Telemetry;
using Xunit;

namespace Social.Tests.Unit.Analytics;

public class EnterpriseAnalyticsTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;

    public EnterpriseAnalyticsTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    // ------------------------------------------------ range / math ---
    [Theory]
    [InlineData("7d", 7)]
    [InlineData("30d", 30)]
    [InlineData("90d", 90)]
    [InlineData("1y", 365)]
    [InlineData("bogus", 30)]
    [InlineData(null, 30)]
    public void ParseRangeDays_MapsKnownRanges(string? range, int expected) =>
        AnalyticsService.ParseRangeDays(range).Should().Be(expected);

    [Theory]
    [InlineData(120, 100, 20)]
    [InlineData(0, 0, 0)]
    [InlineData(5, 0, 100)]
    [InlineData(0, 10, -100)]
    public void DeltaPct_HandlesEdges(double cur, double prev, double expected) =>
        AnalyticsService.DeltaPct(cur, prev).Should().BeApproximately(expected, 0.001);

    [Fact]
    public void TimeUntilNextRunUtc_PointsAtNextMidnightPlusFive()
    {
        var now = new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);
        MetricsAggregationWorker.TimeUntilNextRunUtc(now)
            .Should().Be(TimeSpan.FromHours(12).Add(TimeSpan.FromMinutes(5)));
    }

    // ------------------------------------------------ middleware ---
    [Theory]
    [InlineData("/api/posts/feed", "GET", true)]
    [InlineData("/api/admin/analytics/kpi-summary", "GET", true)]
    [InlineData("/api/health", "GET", false)]
    [InlineData("/api/healthz", "GET", false)]
    [InlineData("/swagger/index.html", "GET", false)]
    [InlineData("/openapi/v1.json", "GET", false)]
    [InlineData("/admin", "GET", false)]
    [InlineData("/admin/login", "GET", false)]
    [InlineData("/_content/app.css", "GET", false)]
    [InlineData("/api/files/logo.png", "GET", false)]
    [InlineData("/api/posts/feed", "OPTIONS", false)]
    public void ShouldCapture_FiltersNonApiTraffic(string path, string method, bool expected)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;
        ctx.Request.Method = method;
        RequestTelemetryMiddleware.ShouldCapture(ctx.Request).Should().Be(expected);
    }

    [Fact]
    public async Task Middleware_LogsApiRequest_WithUserAndTiming()
    {
        var sink = new RequestLogChannel();
        var middleware = new RequestTelemetryMiddleware(_ =>
        {
            _.Response.StatusCode = 201;
            return Task.CompletedTask;
        });
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = "/api/posts";
        ctx.Request.Method = "POST";
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "u1")], "test"));

        await middleware.InvokeAsync(ctx, sink);

        sink.Reader.TryRead(out var log).Should().BeTrue();
        log!.Endpoint.Should().Be("/api/posts");
        log.HttpMethod.Should().Be("POST");
        log.StatusCode.Should().Be(201);
        log.UserId.Should().Be("u1");
        log.DurationMs.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Middleware_SkipsHealthChecks()
    {
        var sink = new RequestLogChannel();
        var middleware = new RequestTelemetryMiddleware(_ => Task.CompletedTask);
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = "/api/health";
        ctx.Request.Method = "GET";

        await middleware.InvokeAsync(ctx, sink);

        sink.Reader.TryRead(out _).Should().BeFalse();
    }

    // ------------------------------------------------ service ---
    [Fact]
    public async Task KpiSummary_AggregatesLiveTables()
    {
        var now = DateTime.UtcNow;
        _context.Users.AddRange(MakeUser("u1"), MakeUser("u2"));
        _context.Posts.Add(new Post
        {
            Id = "p1", UserId = "u1", Content = "hello",
            CreatedAt = now, UpdatedAt = now, IsDeleted = false
        });
        _context.Likes.Add(new Social.Core.Entities.Like
        {
            Id = "l1", PostId = "p1", UserId = "u2", CreatedAt = now
        });
        _context.RequestLogs.AddRange(
            new RequestLog { UserId = "u1", Endpoint = "/api/posts", HttpMethod = "GET", StatusCode = 200, DurationMs = 10, CreatedAt = now },
            new RequestLog { UserId = "u2", Endpoint = "/api/posts", HttpMethod = "GET", StatusCode = 500, DurationMs = 30, CreatedAt = now });
        await _context.SaveChangesAsync();

        var sut = new AnalyticsService(_context);
        var kpi = await sut.GetKpiSummaryAsync();

        kpi.TotalUsers.Should().Be(2);
        kpi.TotalPosts.Should().Be(1);
        kpi.DauToday.Should().Be(2);
        kpi.Interactions24h.Should().Be(1); // 1 like, post itself is not an interaction
        kpi.ErrorRate24hPct.Should().BeApproximately(50, 0.01);
    }

    [Fact]
    public async Task UserGrowth_ReturnsSeededPointPerDay()
    {
        var today = DateTime.UtcNow.Date;
        _context.Users.Add(MakeUser("g1", today.AddDays(-1)));
        _context.DailyMetricSnapshots.Add(new DailyMetricSnapshot { Date = today.AddDays(-1), Dau = 7 });
        await _context.SaveChangesAsync();

        var sut = new AnalyticsService(_context);
        var growth = await sut.GetUserGrowthAsync("7d");

        growth.Days.Should().Be(7);
        growth.Points.Should().HaveCount(7);
        growth.Points.Should().Contain(p => p.NewUsers == 1 && p.Dau == 7);
    }

    [Fact]
    public async Task ApiHealth_ComputesPercentilesAndSlowest()
    {
        var now = DateTime.UtcNow;
        for (var i = 1; i <= 10; i++)
            _context.RequestLogs.Add(new RequestLog
            {
                UserId = "u1", Endpoint = "/api/posts", HttpMethod = "GET",
                StatusCode = i == 10 ? 500 : 200, DurationMs = i * 10, CreatedAt = now
            });
        await _context.SaveChangesAsync();

        var sut = new AnalyticsService(_context);
        var health = await sut.GetApiHealthAsync();

        health.TotalRequests24h.Should().Be(10);
        health.Count2xx.Should().Be(9);
        health.Count5xx.Should().Be(1);
        health.P50Ms.Should().BeApproximately(60, 0.5);
        health.P95Ms.Should().BeApproximately(100, 0.5);
        health.SlowestEndpoints.Should().HaveCount(1);
        health.SlowestEndpoints[0].Endpoint.Should().Be("/api/posts");
    }

    [Fact]
    public async Task SafetyMetrics_GroupsBlocksAndPrivacy()
    {
        _context.Users.AddRange(MakeUser("s1", null, true), MakeUser("s2"));
        _context.BlockUsers.Add(BlockUser.Create("s2", "s1"));
        await _context.SaveChangesAsync();

        var sut = new AnalyticsService(_context);
        var safety = await sut.GetSafetyMetricsAsync(7);

        safety.PrivateAccounts.Should().Be(1);
        safety.PublicAccounts.Should().Be(1);
        safety.BlocksOverTime.Sum(p => p.Blocks).Should().Be(1);
        safety.TopBlocked.Should().ContainSingle(t => t.UserId == "s1");
    }

    private static User MakeUser(string id, DateTime? createdAt = null, bool isPrivate = false) => new()
    {
        Id = id,
        UserName = id,
        NormalizedUserName = id.ToUpperInvariant(),
        Email = $"{id}@test.local",
        NormalizedEmail = $"{id}@TEST.LOCAL",
        FirstName = "Test",
        LastName = "User",
        IsPrivate = isPrivate,
        CreatedAt = createdAt ?? DateTime.UtcNow,
        UpdatedAt = createdAt ?? DateTime.UtcNow
    };
}
