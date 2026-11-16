import type { ServicesMangaPatchMangaDownloadLinkRequest, PatchMangasByMangaIdDownloadLinksByDownloadIdResponses } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';

export const patchMangaDownloadLink = async (downloadLinkId: string, mangaId: string, body: ServicesMangaPatchMangaDownloadLinkRequest) => {
    const { $tranga, $i18n } = useNuxtApp();
    const toast = useToast();
    try {
        await $tranga<PatchMangasByMangaIdDownloadLinksByDownloadIdResponses>(`/mangas/${mangaId}/downloadLinks/${downloadLinkId}`, {
            method: 'patch',
            body: body,
        });
        await refreshNuxtData([ApiKeys.Manga.Manga(mangaId), ApiKeys.Manga.DownloadLinks(mangaId)]);
        toast.add({ title: body.matched ? $i18n.t('toast.setAsSource') : $i18n.t('toast.removedAsSource'), color: 'success' });
    } catch {
        toast.add({
            title: body.matched ? $i18n.t('toast.setDownloadLinkSourceFailed') : $i18n.t('toast.removeDownloadLinkSourceFailed'),
            color: 'error',
        });
    }
};
