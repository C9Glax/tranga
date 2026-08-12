using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Common.Database.Auth;

/// <summary>
/// Lets <c>dotnet ef migrations</c> construct <see cref="AuthContext"/> without booting the full app host (which
/// would otherwise require a reachable RabbitMQ broker just to generate a migration) -
/// <see cref="TrangaDbContext{T}.OnConfiguring"/> still builds the real Postgres connection string from
/// <c>EnvVars</c>, so this only affects design-time tooling, not runtime behaviour.
/// </summary>
public sealed class AuthContextDesignTimeFactory : IDesignTimeDbContextFactory<AuthContext>
{
    public AuthContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AuthContext>().Options);
}
