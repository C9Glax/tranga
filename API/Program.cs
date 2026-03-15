using API;
using Database.MangaContext;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);



builder.Services.AddOpenApi();

builder.Services.AddDbContext<MangaContext>();

WebApplication app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

RouteGroupBuilder routeBuilder = app.MapGroup("/");
Endpoints.AddEndpoints(routeBuilder);

app.UseHttpsRedirection();

app.Run();