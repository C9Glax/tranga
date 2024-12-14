using System.Reflection;
using System.Text.Json.Serialization;
using API;
using API.Schema;
using API.Schema.Jobs;
using API.Schema.MangaConnectors;
using Asp.Versioning;
using Asp.Versioning.Builder;
using Asp.Versioning.Conventions;
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

builder.Services.AddMvc().AddJsonOptions(opts =>
{
    opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    opts.JsonSerializerOptions.Converters.Add(new ApiJsonSerializer());
});

builder.Services.AddApiVersioning(option =>
{
    option.AssumeDefaultVersionWhenUnspecified = true;
    option.DefaultApiVersion = new ApiVersion(2); 
    option.ReportApiVersions = true; 
    option.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new QueryStringApiVersionReader("api-version"),
        new HeaderApiVersionReader("X-Version"),
        new MediaTypeApiVersionReader("x-version"));
})
.AddMvc(options =>
{
    options.Conventions.Add(new VersionByNamespaceConvention());
})
    .AddApiExplorer(options => {
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    opt.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});
builder.Services.ConfigureOptions<NamedSwaggerGenOptions>();


builder.Services.AddDbContext<PgsqlContext>(options =>
    options.UseNpgsql($"Host={Environment.GetEnvironmentVariable("POSTGRES_Host")??"localhost:5432"}; " +
                      $"Database={Environment.GetEnvironmentVariable("POSTGRES_DB")??"postgres"}; " +
                      $"Username={Environment.GetEnvironmentVariable("POSTGRES_USER")??"postgres"}; " +
                      $"Password={Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")??"postgres"}"));

builder.Services.AddControllers();

var app = builder.Build();

ApiVersionSet apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(2))
    .ReportApiVersions()
    .Build();


app.UseCors("AllowAll");

app.MapControllers()
    .WithApiVersionSet(apiVersionSet)
    .MapToApiVersion(2);

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
            $"/swagger/v2/swagger.json", "v2"); 
});

app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    PgsqlContext context = scope.ServiceProvider.GetService<PgsqlContext>()!;
    
    MangaConnector[] connectors =
        [
            new AsuraToon(),
            new Bato(),
            new MangaDex(),
            new MangaHere(),
            new MangaKatana(),
            new MangaLife(),
            new Manganato(),
            new Mangasee(),
            new Mangaworld(),
            new ManhuaPlus(),
            new Weebcentral()
        ];
    MangaConnector[] newConnectors = context.MangaConnectors.Where(c => !connectors.Contains(c)).ToArray();
    context.MangaConnectors.AddRange(newConnectors);
    
    context.Jobs.RemoveRange(context.Jobs.Where(j => j.state == JobState.Completed && j.RecurrenceMs < 1));
    
    context.SaveChanges();
}

app.UseCors("AllowAll");

app.Run();