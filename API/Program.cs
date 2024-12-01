using System.Reflection;
using System.Text.Json.Serialization;
using API;
using API.Schema;
using Asp.Versioning;
using Asp.Versioning.Builder;
using Asp.Versioning.Conventions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Converters;

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

builder.Services.AddMvc().AddJsonOptions(opts => opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

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
            new ("AsuraToon", ["en"], ["asuracomic.net"]),
            new ("Bato", ["en"], ["bato.to"]),
            new ("MangaDex",
            [
                "en", "pt", "pt-br", "it", "de", "ru", "aa", "ab", "ae", "af", "ak", "am", "an", "ar-ae", "ar-bh",
                "ar-dz", "ar-eg", "ar-iq", "ar-jo", "ar-kw", "ar-lb", "ar-ly", "ar-ma", "ar-om", "ar-qa", "ar-sa",
                "ar-sy", "ar-tn", "ar-ye", "ar", "as", "av", "ay", "az", "ba", "be", "bg", "bh", "bi", "bm", "bn", "bo",
                "br", "bs", "ca", "ce", "ch", "co", "cr", "cs", "cu", "cv", "cy", "da", "de-at", "de-ch", "de-de",
                "de-li", "de-lu", "div", "dv", "dz", "ee", "el", "en-au", "en-bz", "en-ca", "en-cb", "en-gb", "en-ie",
                "en-jm", "en-nz", "en-ph", "en-tt", "en-us", "en-za", "en-zw", "eo", "es-ar", "es-bo", "es-cl", "es-co",
                "es-cr", "es-do", "es-ec", "es-es", "es-gt", "es-hn", "es-la", "es-mx", "es-ni", "es-pa", "es-pe",
                "es-pr", "es-py", "es-sv", "es-us", "es-uy", "es-ve", "es", "et", "eu", "fa", "ff", "fi", "fj", "fo",
                "fr-be", "fr-ca", "fr-ch", "fr-fr", "fr-lu", "fr-mc", "fr", "fy", "ga", "gd", "gl", "gn", "gu", "gv",
                "ha", "he", "hi", "ho", "hr-ba", "hr-hr", "hr", "ht", "hu", "hy", "hz", "ia", "id", "ie", "ig", "ii",
                "ik", "in", "io", "is", "it-ch", "it-it", "iu", "iw", "ja", "ja-ro", "ji", "jv", "jw", "ka", "kg", "ki",
                "kj", "kk", "kl", "km", "kn", "ko", "ko-ro", "kr", "ks", "ku", "kv", "kw", "ky", "kz", "la", "lb", "lg",
                "li", "ln", "lo", "ls", "lt", "lu", "lv", "mg", "mh", "mi", "mk", "ml", "mn", "mo", "mr", "ms-bn",
                "ms-my", "ms", "mt", "my", "na", "nb", "nd", "ne", "ng", "nl-be", "nl-nl", "nl", "nn", "no", "nr", "ns",
                "nv", "ny", "oc", "oj", "om", "or", "os", "pa", "pi", "pl", "ps", "pt-pt", "qu-bo", "qu-ec", "qu-pe",
                "qu", "rm", "rn", "ro", "rw", "sa", "sb", "sc", "sd", "se-fi", "se-no", "se-se", "se", "sg", "sh", "si",
                "sk", "sl", "sm", "sn", "so", "sq", "sr-ba", "sr-sp", "sr", "ss", "st", "su", "sv-fi", "sv-se", "sv",
                "sw", "sx", "syr", "ta", "te", "tg", "th", "ti", "tk", "tl", "tn", "to", "tr", "ts", "tt", "tw", "ty",
                "ug", "uk", "ur", "us", "uz", "ve", "vi", "vo", "wa", "wo", "xh", "yi", "yo", "za", "zh-cn", "zh-hk",
                "zh-mo", "zh-ro", "zh-sg", "zh-tw", "zh", "zu"
            ], ["mangadex.org"]),
            new ("MangaHere", ["en"], ["www.mangahere.cc"]),
            new ("MangaKatana", ["en"], ["mangakatana.com"]),
            new ("MangaLife", ["en"], ["manga4life.com"]),
            new ("Manganato", ["en"], ["manganato.com"]),
            new ("Mangasee", ["en"], ["mangasee123.com"]),
            new ("Mangaworld", ["it"], ["www.mangaworld.ac"]),
            new ("ManhuaPlus", ["en"], ["manhuaplus.org"])
        ];
    MangaConnector[] newConnectors = context.MangaConnectors.Where(c => !connectors.Contains(c)).ToArray();
    context.MangaConnectors.AddRange(newConnectors);
    context.SaveChanges();
}

app.Run();