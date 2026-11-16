using System.ComponentModel.DataAnnotations;
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
    /// Try out one step of a (not necessarily saved, not necessarily complete) custom Download-Source against the real
    /// website and report what its selectors extracted. Nothing is stored or downloaded.
    /// </summary>
    /// <param name="req"></param>
    /// <param name="ct"></param>
    /// <response code="200">What the step produced. Problems with the website or the selectors are reported inside the result</response>
    /// <response code="400">The request is invalid (unknown step, missing input or section, invalid settings or section); the validation problem lists what is wrong</response>
    public static async Task<Ok<StepTestResult>> Handle([FromBody] PostCustomDownloaderTestRequest req, CancellationToken ct)
    {
        // The request validated itself before we got here (see PostCustomDownloaderTestRequest.Validate). It only checked
        // the section of the tested step, so only that one is handed on: the engine validates every section it is given.
        using SelectorDownloadExtension extension = req.TestedStep switch
        {
            SelectorDownloaderStep.Search => SelectorDownloadExtension.CreateForTest(req.Settings, search: req.Search),
            SelectorDownloaderStep.Chapters => SelectorDownloadExtension.CreateForTest(req.Settings, chapters: req.Chapters),
            _ => SelectorDownloadExtension.CreateForTest(req.Settings, images: req.Images)
        };
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(Timeout);
        try
        {
            string input = req.Input.Trim();
            StepTestResult result = req.TestedStep switch
            {
                SelectorDownloaderStep.Search => await extension.TestSearchAsync(input, timeout.Token),
                SelectorDownloaderStep.Chapters => await extension.TestChaptersAsync(input, timeout.Token),
                _ => await extension.TestImagesAsync(input, timeout.Token)
            };
            return TypedResults.Ok(result);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return TypedResults.Ok(new StepTestResult { Error = $"The test took longer than {Timeout.TotalSeconds:0} seconds and was cancelled." });
        }
    }

    /// <summary>
    /// Used in <see cref="PostCustomDownloaderTestEndpoint"/>. Carries only what the tested step needs - the general
    /// settings of the website and that step's section - so a step can be tried while the rest of the source is still
    /// empty. It validates itself: the settings and the section of the step are checked, the others are ignored.
    /// </summary>
    public sealed record PostCustomDownloaderTestRequest : IValidatableObject
    {
        /// <summary>The general settings of the website (base URL, headers, rate limit, ...)</summary>
        public required SelectorDownloaderSettings Settings { get; init; }

        /// <summary>What to test: <c>search</c>, <c>chapters</c> or <c>images</c></summary>
        public required string Step { get; init; }

        /// <summary>The search text (search), a series URL (chapters) or a chapter URL (images)</summary>
        public required string Input { get; init; }

        /// <summary>How to find series. Required for the step <c>search</c></summary>
        public SearchSpec? Search { get; init; }

        /// <summary>How to list chapters. Required for the step <c>chapters</c></summary>
        public ChaptersSpec? Chapters { get; init; }

        /// <summary>How to find the pages of a chapter. Required for the step <c>images</c></summary>
        public ImagesSpec? Images { get; init; }

        /// <summary>The step as enum. Only meaningful once the request is valid.</summary>
        internal SelectorDownloaderStep TestedStep => Enum.Parse<SelectorDownloaderStep>(Step, ignoreCase: true);

        /// <inheritdoc />
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Enum.TryParse(Step, ignoreCase: true, out SelectorDownloaderStep step) || !Enum.IsDefined(step))
            {
                yield return new ValidationResult($"Unknown step '{Step}'. Use search, chapters or images.", [nameof(Step)]);
                yield break;
            }

            if (string.IsNullOrWhiteSpace(Input))
                yield return new ValidationResult("Input is required: a search text, a series URL or a chapter URL.", [nameof(Input)]);

            // The settings are validated by their own datatype; what is left is the section of the tested step.
            switch (step)
            {
                case SelectorDownloaderStep.Search:
                    if (Search is null)
                        yield return new ValidationResult("The search section is required to test the search.", [nameof(Search)]);
                    else
                        foreach (ValidationResult result in Search.Validate(nameof(Search), Settings.Format))
                            yield return result;
                    break;
                case SelectorDownloaderStep.Chapters:
                    if (Chapters is null)
                        yield return new ValidationResult("The chapters section is required to test the chapter list.", [nameof(Chapters)]);
                    else
                        foreach (ValidationResult result in Chapters.Validate(nameof(Chapters), Settings.Format, Settings.SeriesUrlGroups()))
                            yield return result;
                    break;
                default:
                    if (Images is null)
                        yield return new ValidationResult("The images section is required to test the pages.", [nameof(Images)]);
                    else
                        foreach (ValidationResult result in Images.Validate(nameof(Images), Settings.Format))
                            yield return result;
                    break;
            }
        }
    }
}
