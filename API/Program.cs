using System.Text.Json.Serialization;
using API;
using Database.DownloadContext;
using Database.MangaContext;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(opts =>
{
    opts.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
}).ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddDbContext<MangaContext>();
builder.Services.AddDbContext<DownloadContext>();

WebApplication app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

RouteGroupBuilder routeBuilder = app.MapGroup("/");
Endpoints.AddEndpoints(routeBuilder);

app.UseHttpsRedirection();

app.Run();