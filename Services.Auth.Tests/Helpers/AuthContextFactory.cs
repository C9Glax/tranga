using Common.Database.Auth;
using Microsoft.EntityFrameworkCore;

namespace Services.Auth.Tests.Helpers;

/// <summary>
/// Creates isolated <see cref="AuthContext"/> instances backed by a per-call temp-file Sqlite database for
/// tests, mirroring Services.Libraries.Tests/Helpers/LibrariesContextFactory.cs.
/// </summary>
public static class AuthContextFactory
{
    private static readonly string RootDirectory = Path.Combine(Path.GetTempPath(), "TrangaTests", "Services.Auth.Tests");

    public static AuthContext Create()
    {
        Directory.CreateDirectory(RootDirectory);
        string dbPath = Path.Combine(RootDirectory, $"{Guid.NewGuid():N}.db");
        DbContextOptions<AuthContext> options = new DbContextOptionsBuilder<AuthContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        AuthContext context = new(options);
        context.Database.EnsureCreated();
        return context;
    }
}
