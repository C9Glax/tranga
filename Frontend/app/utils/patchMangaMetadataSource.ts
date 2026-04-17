import type { PatchMangasByMangaIdMetadataByMetadataIdResponses } from '~/api/tranga';

export const patchMangaMetadataSource = async (metadataId: string, mangaId: string) => {
    const { $tranga } = useNuxtApp();
    const toast = useToast();
    try {
        await $tranga<PatchMangasByMangaIdMetadataByMetadataIdResponses>(`/mangas/${mangaId}/metadata/${metadataId}`, { method: 'patch' });
        await refreshNuxtData([ApiKeys.Metadata(metadataId), ApiKeys.MetadataManga(metadataId)]);
        toast.add({ title: 'Set as Source!', color: 'success' });
    } catch {
        toast.add({ title: 'Could not set Metadata as Source!', color: 'error' });
    }
};
