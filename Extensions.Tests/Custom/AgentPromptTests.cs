using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using Extensions.Custom;

namespace Extensions.Tests.Custom;

/// <summary>
/// <c>Extensions/Custom/Examples/agent-prompt.md</c> is what users hand to an AI agent to have it write a definition, and
/// the frontend links to it. Its JSON skeleton has to keep matching the definition, or the agents learn field names that
/// do not exist.
/// </summary>
public sealed class AgentPromptTests
{
    private static string ExamplesDirectory
    {
        get
        {
            // The tests run from bin/<config>/<tfm>; the prompt is a file of the repository, not part of any build output.
            for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            {
                string candidate = Path.Combine(directory.FullName, "Extensions", "Custom", "Examples");
                if (File.Exists(Path.Combine(candidate, "agent-prompt.md")))
                    return candidate;
            }

            throw new FileNotFoundException("Extensions/Custom/Examples/agent-prompt.md was not found above " + AppContext.BaseDirectory);
        }
    }

    private static string Prompt => File.ReadAllText(Path.Combine(ExamplesDirectory, "agent-prompt.md"));

    private static string Skeleton
    {
        get
        {
            Match match = Regex.Match(Prompt, "```json\\s*(?<json>.*?)```", RegexOptions.Singleline);
            Assert.True(match.Success, "The prompt has no ```json block with the skeleton.");
            return match.Groups["json"].Value;
        }
    }

    /// <summary>The camelCase names of everything a definition can have, as written in JSON.</summary>
    private static HashSet<string> PropertyNamesOf(params Type[] types) => types
        .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() is null && p.GetMethod?.IsPublic == true)
        .Select(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name))
        .ToHashSet();

    private static void CollectNames(JsonElement element, HashSet<string> names)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return;
        foreach (JsonProperty property in element.EnumerateObject())
        {
            names.Add(property.Name);
            // The keys of the headers are header names, not properties of the definition.
            if (property.Name != "headers")
                CollectNames(property.Value, names);
        }
    }

    [Fact]
    public void TheSkeletonIsAValidDefinition()
    {
        SelectorDownloaderDefinition? definition = SelectorDownloaderJson.Deserialize(Skeleton);

        Assert.NotNull(definition);
        Assert.Empty(definition.Normalized().Validate());
    }

    [Fact]
    public void TheSkeletonShowsEveryFieldAndNothingElse()
    {
        HashSet<string> known = PropertyNamesOf(typeof(SelectorDownloaderDefinition), typeof(SearchSpec), typeof(SeriesSpec), typeof(ChaptersSpec), typeof(ImagesSpec), typeof(SelectorField));
        HashSet<string> shown = [];
        CollectNames(JsonDocument.Parse(Skeleton).RootElement, shown);

        Assert.Empty(known.Except(shown)); // a field was added to the definition, the prompt does not know it
        Assert.Empty(shown.Except(known)); // the prompt shows a field that does not exist (anymore)
    }

    [Fact]
    public void EveryExampleIsMentioned() => Assert.All(
        Directory.GetFiles(ExamplesDirectory, "*.json").Select(Path.GetFileName),
        example => Assert.Contains($"/Extensions/Custom/Examples/{example}", Prompt));

    [Fact]
    public void ItAsksTheUserForTheWebsiteInsteadOfHavingAPlaceholderToFillIn()
    {
        Assert.Contains("Start by asking the user which website", Prompt);
        Assert.DoesNotContain("<WEBSITE>", Prompt);
    }

    [Fact]
    public void ItExplainsBothFormats()
    {
        Assert.Contains("**`html`**", Prompt);
        Assert.Contains("**`json`**", Prompt);
    }

    [Theory]
    [InlineData("weebcentral.json")]
    [InlineData("mangatown.json")]
    [InlineData("mangauno.json")]
    public void TheExamplesItPointsToExist(string example)
    {
        Assert.Contains($"/Extensions/Custom/Examples/{example}", Prompt);
        Assert.True(File.Exists(Path.Combine(ExamplesDirectory, example)));
    }
}
