using Extensions;
using Extensions.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Database.Helpers;
using Services.Manga.Entities;
using Services.Manga.Helpers;

namespace Services.Manga.Features.Manga;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class PostMangaDownloadLinkEndpoint
{
    /// <summary>
    /// Manually add a Download-Link for a Manga by pasting the manga's page URL on a Download-Extension's site.
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="mangaId">ID of Manga</param>
    /// <param name="req"></param>
    /// <param name="ct"></param>
    /// <response code="200">The Download-Link was added, or already existed and the Manga was set to monitored</response>
    /// <response code="404">Manga with requested ID does not exist</response>
    /// <response code="400">The extension is unknown, the URL does not match the extension's expected shape, or the extension could not fetch chapters for the resolved series</response>
    public static async Task<Results<Ok<MangaDownloadLink>, NotFound, BadRequest<string>>> Handle(
        [FromServices] MangaContext mangaContext, [FromRoute] Guid mangaId, [FromBody] PostMangaDownloadLinkRequest req, CancellationToken ct)
    {
        if (await mangaContext.GetManga(mangaId, ct) is not { } source)
            return TypedResults.NotFound();

        if (DownloadExtensionsCollection.GetExtension(req.DownloadExtensionId) is not { } extension)
            return TypedResults.BadRequest($"Unknown download extension '{req.DownloadExtensionId}'.");

        if (extension.ParseIdentifierFromUrl(req.Url) is not { } identifier)
            return TypedResults.BadRequest($"That doesn't look like a {extension.Name} manga page URL.");

        if (await mangaContext.MangaDownloadLinks.FirstOrDefaultAsync(m =>
                m.MangaId == mangaId && m.DownloadLink.DownloadExtension == req.DownloadExtensionId && m.DownloadLink.Identifier == identifier, ct) is { } existingLink)
        {
            source.Manga.Monitored = true;
            await mangaContext.SaveChangesAsync(ct);
            return TypedResults.Ok(existingLink.ToDTO());
        }

        DbDownloadLink downloadLink = new()
        {
            DownloadExtension = req.DownloadExtensionId,
            Identifier = identifier,
            Series = source.Metadata.Series,
            Url = req.Url
        };

        List<ChapterInfo>? chapters;
        try
        {
            chapters = await extension.GetChapters(downloadLink.ToMangaInfo(), ct);
        }
        catch
        {
            chapters = null;
        }

        if (chapters is null)
            return TypedResults.BadRequest($"Could not fetch chapters from {extension.Name} for that URL - double-check the link is correct.");

        DbMangaDownloadLinks mangaDownloadLinks = new()
        {
            DownloadLink = downloadLink,
            Manga = source.Manga,
            Matched = false,
            Priority = 0
        };

        await mangaContext.AddAsync(mangaDownloadLinks, ct);
        await mangaContext.SaveChangesAsync(ct);

        return TypedResults.Ok(mangaDownloadLinks.ToDTO());
    }

    /// <summary>
    /// Used in <see cref="PostMangaDownloadLinkEndpoint"/>
    /// </summary>
    /// <param name="DownloadExtensionId">ID of the Download-Extension the URL belongs to</param>
    /// <param name="Url">The manga's page URL on that extension's site</param>
    public sealed record PostMangaDownloadLinkRequest(Guid DownloadExtensionId, string Url);
}
