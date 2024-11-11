using API.Schema;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddDbContext<PgsqlContext>(options =>
    options.UseNpgsql($"Host={Environment.GetEnvironmentVariable("POSTGRES_Host")}; " +
                      $"Database={Environment.GetEnvironmentVariable("POSTGRES_DB")??"postgres"}; " +
                      $"Username={Environment.GetEnvironmentVariable("POSTGRES_USER")??"postgres"}; " +
                      $"Password={Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")??"postgres"}"));

builder.Services.AddControllers();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

app.MapControllers()
    .WithApiVersionSet(app.NewApiVersionSet()
        .HasApiVersion(new ApiVersion(2))
        .ReportApiVersions()
        .Build());

app.UseHttpsRedirection();

app.Run();