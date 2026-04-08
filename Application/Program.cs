using Application.Services;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<MangaService>();
builder.Services.AddHostedService<TasksService>();

IHost app = builder.Build();

app.Run();