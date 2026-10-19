using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicSlot.IntegrationTests;

/// <summary>Hosts the API in memory against a throw-away SQLite file and a controllable clock.</summary>
public sealed class ClinicSlotApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Monday 2 March 2026, 08:00 UTC (London is on GMT).</summary>
    public static readonly DateTimeOffset Now = new(2026, 3, 2, 8, 0, 0, TimeSpan.Zero);

    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"clinicslot-tests-{Guid.NewGuid():N}.db");

    public TestClock Clock { get; } = new(Now);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:ClinicSlot"] = $"Data Source={_databasePath}",
            ["Database:MigrateOnStartup"] = "true",
            ["Logging:LogLevel:Default"] = "Warning",
        }));
        builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(Clock));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { string.Empty, "-shm", "-wal", "-journal" })
        {
            File.Delete(_databasePath + suffix);
        }
    }
}

public sealed class TestClock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Current { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Current;
}
