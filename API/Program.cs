using API;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

WebApplication app = builder.Build();

app.MapOpenApi();

RouteGroupBuilder routeBuilder = app.MapGroup("/");
Endpoints.AddEndpoints(routeBuilder);

app.UseHttpsRedirection();

app.Run();