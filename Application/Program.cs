using Application.Services;
using Database;
using Database.MangaContext;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<MangaContext>(opts => opts.Configure(DatabaseContextOptionsBuilder.DbType.Postgresql));

builder.Services.AddHostedService<ApiService>();

IHost app = builder.Build();

using (MangaContext context = app.Services.CreateScope().ServiceProvider.GetRequiredService<MangaContext>())
{
    Task.WaitAll(context.ApplyMigrations());
}

app.Run();