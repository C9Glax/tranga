using Common.Database.Auth;
using Microsoft.EntityFrameworkCore;

namespace Services.Auth.ResetPasswordTool.Tests.Helpers;

/// <summary>
/// Creates isolated <see cref="AuthContext"/> instances backed by a per-call temp-file Sqlite database for
/// tests, mirroring Services.Auth.Tests/Helpers/AuthContextFactory.cs.
/// </summary>
public static class AuthContextFactory
{
    private static readonly string RootDirectory = Path.Combine(Path.GetTempPath(), "TrangaTests", "Services.Auth.ResetPasswordTool.Tests");

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
