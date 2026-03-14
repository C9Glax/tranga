using System.Net.Http.Headers;
using NJsonSchema.CodeGeneration.CSharp;
using NSwag;
using NSwag.CodeGeneration.CSharp;

namespace NSwagClients;

internal static class Generator
{
    public enum Type
    {
        Json,
        Yaml
    }
    
    public static async Task GenerateFromUrl(string documentUrl, string name, CancellationToken ct, Type type = Type.Json)
    {
        try
        {
            OpenApiDocument document = type switch
            {
                Type.Yaml => await OpenApiYamlDocument.FromUrlAsync(documentUrl, ct),
                Type.Json => await OpenApiDocument.FromUrlAsync(documentUrl, ct),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
            await Generate(document, name, ct);
        }
        catch (HttpRequestException) // There was an error while requesting the document using NSwags HttpClient
        {
            HttpClient client = new()
            {
                DefaultRequestHeaders =
                {
                    UserAgent = { new ProductInfoHeaderValue(new ProductHeaderValue("TrangaClientGenerator", "1.0")) }
                }
            };
            string str = await client.GetStringAsync(documentUrl, ct);
            await GenerateFrom(str, name, ct, type);
        }
    }
    
    public static async Task GenerateFromFile(string filePath, string name, CancellationToken ct, Type type = Type.Json)
    {
        OpenApiDocument document = type switch
        {
            Type.Yaml => await OpenApiYamlDocument.FromFileAsync(filePath, ct),
            Type.Json => await OpenApiDocument.FromFileAsync(filePath, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
        await Generate(document, name, ct);
    }
    
    public static async Task GenerateFrom(string str, string name, CancellationToken ct, Type type = Type.Json)
    {
        OpenApiDocument document = type switch
        {
            Type.Yaml => await OpenApiYamlDocument.FromYamlAsync(str, ct),
            Type.Json => await OpenApiDocument.FromJsonAsync(str, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
        await Generate(document, name, ct);
    }

    private static async Task Generate(OpenApiDocument document, string name, CancellationToken ct)
    {
        CSharpClientGeneratorSettings settings = new()
        {
            ClassName = $"{name}ApiClient",
            ResponseClass = $"{name}ApiResponse",
            ExceptionClass = $"{name}ApiException",
            GenerateExceptionClasses = true,
            GenerateOptionalParameters = true,
            GenerateResponseClasses = true,
            GenerateDtoTypes = true,
            GenerateClientClasses = true,
            CSharpGeneratorSettings =
            {
                Namespace = $"NSwagClients.GeneratedClients.{name}",
                JsonLibrary = CSharpJsonLibrary.SystemTextJson,
                GenerateNullableReferenceTypes = true,
                GenerateOptionalPropertiesAsNullable = true,
                UseRequiredKeyword = true,
                JsonLibraryVersion = 10,
                GenerateDefaultValues = true,
                GenerateDataAnnotations = true
            },
        };

        CSharpClientGenerator generator = new (document, settings);

        string code = generator.GenerateFile();

        Directory.CreateDirectory("../../../GeneratedClients");
        string path = $"../../../GeneratedClients/{settings.ClassName}.cs";
        await File.WriteAllTextAsync(path, code, ct);
    }
}