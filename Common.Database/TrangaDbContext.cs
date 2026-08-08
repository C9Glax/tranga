using Common.Settings;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Common.Database;

public abstract class TrangaDbContext<T> : DbContext where T : TrangaDbContext<T>
{
    protected TrangaDbContext() { }

    /// <summary>
    /// Lets a derived context accept already-configured <paramref name="options"/> (e.g. an in-memory or
    /// Sqlite provider used by tests), bypassing Postgres. In production, <c>AddDbContext&lt;T&gt;()</c> is
    /// registered without a configuring action, so the <see cref="DbContextOptions{TContext}"/> DI injects here
    /// (or via the parameterless constructor) are unconfigured and <see cref="OnConfiguring"/> falls through
    /// to Postgres below.
    /// </summary>
    protected TrangaDbContext(DbContextOptions<T> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
            return;

        NpgsqlConnectionStringBuilder connectionStringBuilder = new()
        {
            Host = EnvVars.DBHost ?? EnvVars.POSTGRES_HOST,
            Port = EnvVars.POSTGRES_PORT,
            Database = EnvVars.DBName,
            Username = EnvVars.DBUser ?? EnvVars.POSTGRES_USER,
            Password = EnvVars.DBPass ?? EnvVars.POSTGRES_PASSWORD,
            ConnectionLifetime = EnvVars.DBConnectionLifetime,
            Timeout = EnvVars.DBConnectionTimeout,
            ReadBufferSize = 65536,
            WriteBufferSize = 65536,
            CommandTimeout = EnvVars.DBCommandTimeout,
            ApplicationName = "Tranga"
        };
        optionsBuilder.UseNpgsql(connectionStringBuilder.ConnectionString);
    }
}