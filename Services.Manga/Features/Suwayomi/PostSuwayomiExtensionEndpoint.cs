using Common.Services.Events;
using Common.Services.Events.Events;
using Extensions;
using Extensions.Extensions.Suwayomi;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Services.Manga.Database;

namespace Services.Manga.Features.Suwayomi;

/// <summary>
/// Installing, updating and uninstalling a Suwayomi extension. All three change the set of available sources, so each
/// refreshes this service's extension collection and tells the other services to do the same.
/// </summary>
internal abstract class PostSuwayomiExtensionEndpoint
{
    /// <summary>
    /// Install a Suwayomi extension
    /// </summary>
    /// <param name="eventPublisher"></param>
    /// <param name="pkgName">Package name of the extension, as returned by the extension catalogue</param>
    /// <param name="ct"></param>
    /// <response code="200">The extension was installed and its sources are now available</response>
    /// <response code="503">The Suwayomi sidecar is unreachable or refused the install</response>
    public static Task<Results<Ok, StatusCodeHttpResult>> Install([FromServices]EventPublisher eventPublisher, [FromRoute]string pkgName, CancellationToken ct) =>
        Apply(() => SuwayomiExtensionManager.InstallAsync(pkgName, ct), eventPublisher, ct);

    /// <summary>
    /// Update a Suwayomi extension
    /// </summary>
    /// <param name="eventPublisher"></param>
    /// <param name="pkgName">Package name of the extension</param>
    /// <param name="ct"></param>
    /// <response code="200">The extension was updated</response>
    /// <response code="503">The Suwayomi sidecar is unreachable or refused the update</response>
    public static Task<Results<Ok, StatusCodeHttpResult>> Update([FromServices]EventPublisher eventPublisher, [FromRoute]string pkgName, CancellationToken ct) =>
        Apply(() => SuwayomiExtensionManager.UpdateAsync(pkgName, ct), eventPublisher, ct);

    /// <summary>
    /// Uninstall a Suwayomi extension
    /// </summary>
    /// <param name="eventPublisher"></param>
    /// <param name="mangaContext"></param>
    /// <param name="pkgName">Package name of the extension</param>
    /// <param name="ct"></param>
    /// <remarks>
    /// Download links pointing at this extension's sources are deleted with it. Mangas, chapters, metadata and
    /// already downloaded files are kept. Re-installing the extension does not restore the deleted links.
    /// </remarks>
    /// <response code="200">The extension was uninstalled</response>
    /// <response code="503">The Suwayomi sidecar is unreachable or refused the uninstall</response>
    public static async Task<Results<Ok, StatusCodeHttpResult>> Uninstall([FromServices]EventPublisher eventPublisher, [FromServices]MangaContext mangaContext, [FromRoute]string pkgName, CancellationToken ct)
    {
        // The sources an extension provides are only known to the sidecar, so diff the source list around the uninstall.
        HashSet<Guid> before = (await SuwayomiExtensionManager.GetSourcesAsync(ct))?.Select(s => s.ExtensionId).ToHashSet() ?? [];

        Results<Ok, StatusCodeHttpResult> result = await Apply(() => SuwayomiExtensionManager.UninstallAsync(pkgName, ct), eventPublisher, ct);
        if (result.Result is not Ok)
            return result;

        // An unreachable sidecar after a successful uninstall means the diff is unknowable; delete nothing rather than guess.
        if (await SuwayomiExtensionManager.GetSourcesAsync(ct) is { } after)
        {
            HashSet<Guid> remaining = after.Select(s => s.ExtensionId).ToHashSet();
            await DeleteLinksOfRemovedExtensions(mangaContext, before.Where(id => !remaining.Contains(id)).ToList(), ct);
        }

        return result;
    }

    /// <summary>
    /// Deletes every download link that refers to one of <paramref name="removedExtensionIds"/>: the manga's
    /// <see cref="DbMangaDownloadLinks"/> matches, the <see cref="DbDownloadLink"/> candidates themselves and the
    /// <see cref="DbChapterDownloadLink"/>s that were never downloaded.
    /// <para>
    /// Chapter links that already hold a downloaded file are kept: they are what marks the chapter as downloaded, and
    /// nothing reads them again once the file exists. Mangas, chapters, metadata and files are left untouched.
    /// </para>
    /// </summary>
    /// <returns>The number of <see cref="DbDownloadLink"/> candidates deleted.</returns>
    internal static async Task<int> DeleteLinksOfRemovedExtensions(MangaContext mangaContext, ICollection<Guid> removedExtensionIds, CancellationToken ct)
    {
        if (removedExtensionIds.Count == 0)
            return 0;

        await using IDbContextTransaction transaction = await mangaContext.Database.BeginTransactionAsync(ct);

        await mangaContext.MangaDownloadLinks
            .Where(l => removedExtensionIds.Contains(l.DownloadLink.DownloadExtension))
            .ExecuteDeleteAsync(ct);

        int deletedLinks = await mangaContext.DownloadLinks
            .Where(l => removedExtensionIds.Contains(l.DownloadExtension))
            .ExecuteDeleteAsync(ct);

        await mangaContext.ChapterDownloadLinks
            .Where(l => removedExtensionIds.Contains(l.DownloadExtension) && l.FileId == null)
            .ExecuteDeleteAsync(ct);

        await transaction.CommitAsync(ct);
        return deletedLinks;
    }

    private static async Task<Results<Ok, StatusCodeHttpResult>> Apply(Func<Task<bool>> action, EventPublisher eventPublisher, CancellationToken ct)
    {
        if (!await action())
            return TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable);

        await DownloadExtensionsCollection.RefreshSidecarExtensionsAsync(ct);
        await eventPublisher.PublishAsync(new SuwayomiSourcesChangedEvent(), ct);

        return TypedResults.Ok();
    }
}
