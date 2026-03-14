using API;
using Database.MangaContext;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<MangaContext>();

WebApplication app = builder.Build();

app.MapOpenApi();

RouteGroupBuilder routeBuilder = app.MapGroup("/");
Endpoints.AddEndpoints(routeBuilder);

app.UseHttpsRedirection();

app.Run();