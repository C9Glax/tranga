import type { PatchMangasByMangaIdMetadataByMetadataIdResponses } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';

export const patchMangaMetadataSource = async (metadataId: string, mangaId: string) => {
    const { $tranga, $i18n } = useNuxtApp();
    const toast = useToast();
    try {
        await $tranga<PatchMangasByMangaIdMetadataByMetadataIdResponses>(`/mangas/${mangaId}/metadata/${metadataId}`, { method: 'patch' });
        await refreshNuxtData([
            ApiKeys.Manga.Metadata.Entry(metadataId),
            ApiKeys.Manga.Manga(mangaId),
            ApiKeys.Manga.List,
            ApiKeys.Manga.RelatedMetadata(mangaId),
            ApiKeys.Manga.Metadata.RelatedManga(metadataId),
            ApiKeys.Manga.Metadata.Manga(metadataId),
        ]);
        toast.add({ title: $i18n.t('toast.setAsSource'), color: 'success' });
    } catch {
        toast.add({ title: $i18n.t('toast.metadataSetFailed'), color: 'error' });
    }
};
