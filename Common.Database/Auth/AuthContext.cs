using Microsoft.EntityFrameworkCore;

namespace Common.Database.Auth;

/// <summary>
/// EF Core context for the credential/API-key tables. Lives in <c>Common.Database</c> (rather than owned by a
/// single service, as other contexts are) because every service needs read access to <see cref="ApiKeys"/> to
/// validate the <c>X-Api-Key</c> header - only <c>Services.Auth</c> runs migrations against it or writes to it.
/// </summary>
public sealed class AuthContext(DbContextOptions<AuthContext> options) : TrangaDbContext<AuthContext>(options)
{
    public DbSet<DbCredential> Credentials { get; init; }

    public DbSet<DbApiKey> ApiKeys { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbCredential>()
            .HasKey(c => c.Id);

        modelBuilder.Entity<DbApiKey>()
            .HasKey(k => k.Id);
        modelBuilder.Entity<DbApiKey>()
            .HasIndex(k => k.KeyHash)
            .IsUnique();
    }
}
