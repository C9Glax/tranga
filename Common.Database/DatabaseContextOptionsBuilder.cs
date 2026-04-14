using Microsoft.EntityFrameworkCore;
using Npgsql;
using Settings;

namespace Common.Database;

public static class DatabaseContextOptionsBuilder
{
    public enum DbType
    {
        Postgresql
    }

    public static void Configure(this DbContextOptionsBuilder builder, DbType? type)
    {
        switch(type)
        {
            case DbType.Postgresql:
            default:
                builder.ConfigurePostgres();
                break;
        };
    }

    private static void ConfigurePostgres(this DbContextOptionsBuilder builder)
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
        builder.UseNpgsql(connectionStringBuilder.ConnectionString);
    }
}