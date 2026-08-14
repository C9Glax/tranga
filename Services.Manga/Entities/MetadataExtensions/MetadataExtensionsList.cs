using Extensions;

namespace Services.Manga.Entities.MetadataExtensions;

/// <summary>
/// The set of metadata extensions available to the manga service, exposed as an API-friendly list.
/// Projected from the live <see cref="MetadataExtensionsCollection"/> so it cannot drift from the extensions
/// that actually run.
/// </summary>
public sealed record MetadataExtensionsList
{
    /// <summary>The available metadata extensions.</summary>
    public MetadataExtension[] Extensions { get; init; } = MetadataExtensionsCollection.Extensions
        .Select(extension => new MetadataExtension
        {
            MetadataExtensionId = extension.Identifier,
            Name = extension.Name,
            IconUrl = extension.IconUrl
        })
        .ToArray();
}
