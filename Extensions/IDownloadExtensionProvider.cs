namespace Extensions;

/// <summary>
/// A source of <see cref="IDownloadExtension"/> instances. This is the seam that makes the download-extension
/// system modular: <see cref="DownloadExtensionsCollection"/> only knows about providers, never about where an
/// extension comes from.
/// <para>
/// Implemented today by <see cref="BuiltInDownloadExtensionProvider"/> (extensions compiled into Tranga).
/// Planned follow-ups:
/// <list type="bullet">
/// <item>a plugin provider that loads <see cref="IDownloadExtension"/> implementations from assemblies in a plugin directory,</item>
/// <item>a declarative provider that builds extensions from stored definitions (CSS/XPath selectors for search,
/// series page, chapter list and image list) which users paste into a form in the frontend.</item>
/// </list>
/// Extension identifiers are persisted on download links, so a provider must hand out stable identifiers.
/// </para>
/// </summary>
public interface IDownloadExtensionProvider
{
    /// <summary>Human-readable name of the provider, used for logging.</summary>
    string Name { get; }

    /// <summary>
    /// Returns the complete set of extensions this provider currently offers. Called on every
    /// <see cref="DownloadExtensionsCollection.RefreshAsync"/>; the result replaces whatever the provider returned before.
    /// </summary>
    Task<IReadOnlyList<IDownloadExtension>> LoadAsync(CancellationToken ct);
}
