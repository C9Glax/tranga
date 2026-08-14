using Extensions;
using Extensions.Extensions.Suwayomi;

namespace Services.Manga.Entities.DownloadExtensions;

/// <summary>
/// The set of download extensions available to the manga service, exposed as an API-friendly list.
/// <para>
/// Projected from the live <see cref="DownloadExtensionsCollection"/> rather than hardcoded, because the
/// Suwayomi-backed extensions are discovered at runtime and change as the user installs or removes them.
/// </para>
/// </summary>
public sealed record DownloadExtensionsList
{
    /// <summary>The available download extensions.</summary>
    public DownloadExtension[] Extensions { get; init; } = DownloadExtensionsCollection.Extensions
        .Select(extension => new DownloadExtension
        {
            DownloadExtensionsId = extension.Identifier,
            Name = extension.Name,
            IconUrl = extension.IconUrl,
            IsSuwayomiSource = extension is SuwayomiSource
        })
        .ToArray();
}
