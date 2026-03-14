WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

WebApplication app = builder.Build();

app.MapOpenApi();

app.UseHttpsRedirection();

app.Run();