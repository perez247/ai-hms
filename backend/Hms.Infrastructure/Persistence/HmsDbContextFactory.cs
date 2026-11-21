using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hms.Infrastructure.Persistence;

/// <summary>Design-time factory so EF migrations can build without starting the API.</summary>
public sealed class HmsDbContextFactory : IDesignTimeDbContextFactory<HmsDbContext>
{
    public HmsDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("HMS_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=hms;Username=hms;Password=hms_dev_password";
        var options = new DbContextOptionsBuilder<HmsDbContext>()
            .UseNpgsql(connection)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new HmsDbContext(options);
    }
}
