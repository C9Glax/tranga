using Common.Settings;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Common.Database;

public abstract class TrangaDbContext<T> : DbContext where T : TrangaDbContext<T>
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
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