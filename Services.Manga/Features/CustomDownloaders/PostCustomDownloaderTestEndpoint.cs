using Extensions.Custom;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Services.Manga.Features.CustomDownloaders;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class PostCustomDownloaderTestEndpoint
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(90);

    /// <summary>
    /// Try out one step of a (not necessarily saved) custom Download-Source definition against the real website and
    /// report what its selectors extracted. Nothing is stored or downloaded.
    /// </summary>
    /// <param name="req"></param>
    /// <param name="ct"></param>
    /// <response code="200">What the step produced. Problems with the website or the selectors are reported inside the result</response>
    /// <response code="400">The step is unknown, the input is missing, or the definition is invalid for that step</response>
    public static async Task<Results<Ok<StepTestResult>, BadRequest<string[]>>> Handle([FromBody] PostCustomDownloaderTestRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse(req.Step, ignoreCase: true, out SelectorDownloaderStep step) || !Enum.IsDefined(step))
            return TypedResults.BadRequest(new[] { $"Unknown step '{req.Step}'. Use search, chapters or images." });
        if (string.IsNullOrWhiteSpace(req.Input))
            return TypedResults.BadRequest(new[] { "Input is required: a search text, a series URL or a chapter URL." });

        SelectorDownloaderDefinition definition = req.Definition.Id == Guid.Empty ? req.Definition with { Id = Guid.NewGuid() } : req.Definition;
        if (definition.Validate(step) is { Count: > 0 } errors)
            return TypedResults.BadRequest(errors.ToArray());

        using SelectorDownloadExtension extension = SelectorDownloadExtension.CreateForTest(definition, step);
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(Timeout);
        try
        {
            StepTestResult result = step switch
            {
                SelectorDownloaderStep.Search => await extension.TestSearchAsync(req.Input.Trim(), timeout.Token),
                SelectorDownloaderStep.Chapters => await extension.TestChaptersAsync(req.Input.Trim(), timeout.Token),
                _ => await extension.TestImagesAsync(req.Input.Trim(), timeout.Token)
            };
            return TypedResults.Ok(result);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return TypedResults.Ok(new StepTestResult { Error = $"The test took longer than {Timeout.TotalSeconds:0} seconds and was cancelled." });
        }
    }

    /// <summary>
    /// Used in <see cref="PostCustomDownloaderTestEndpoint"/>
    /// </summary>
    /// <param name="Definition">The definition to test. Only the general settings and the tested step have to be filled in</param>
    /// <param name="Step">What to test: <c>search</c>, <c>chapters</c> or <c>images</c></param>
    /// <param name="Input">The search text (search), a series URL (chapters) or a chapter URL (images)</param>
    public sealed record PostCustomDownloaderTestRequest(SelectorDownloaderDefinition Definition, string Step, string Input);
}
