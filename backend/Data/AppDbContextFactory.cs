using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace backend.Data;

// Used only by dotnet-ef at design time (migrations add / script).
// Uses SUPABASE_DB_CONNECTION if set, otherwise a placeholder; generating
// migrations or scripts never opens a connection.
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("SUPABASE_DB_CONNECTION")
            ?? "Host=localhost;Database=postgres;Username=postgres;Password=placeholder";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connection)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }
}
