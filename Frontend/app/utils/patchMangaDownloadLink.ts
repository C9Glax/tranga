import type { PatchMangaDownloadLinkRequest, PatchMangasByMangaIdDownloadLinksByDownloadIdResponses } from '~/api/trangaApi';

export const patchMangaDownloadLink = async (downloadLinkId: string, mangaId: string, body: PatchMangaDownloadLinkRequest) => {
    const { $tranga } = useNuxtApp();
    const toast = useToast();
    try {
        await $tranga<PatchMangasByMangaIdDownloadLinksByDownloadIdResponses>(`/mangas/${mangaId}/downloadLinks/${downloadLinkId}`, {
            method: 'patch',
            body: body,
        });
        await refreshNuxtData([ApiKeys.Manga(mangaId), ApiKeys.MangaDownloadLinks(mangaId)]);
        toast.add({ title: body.matched ? 'Set as Source!' : 'Removed as Source', color: 'success' });
    } catch {
        toast.add({
            title: body.matched ? 'Could not set DownloadLink as Source!' : 'Could not remove DownloadLink as Source!',
            color: 'error',
        });
    }
};
