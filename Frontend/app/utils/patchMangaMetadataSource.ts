import type { PatchMangasByMangaIdMetadataResponses } from '~/api/trangaApi';

export const patchMangaMetadataSource = async (metadataId: string, mangaId: string) => {
    const { $tranga } = useNuxtApp();
    const toast = useToast();
    try {
        await $tranga<PatchMangasByMangaIdMetadataResponses>(`/mangas/${mangaId}/metadata`, {
            method: 'patch',
            body: { metadataId: metadataId },
        });
        await refreshNuxtData(ApiKeys.Metadata(metadataId));
    } catch {
        toast.add({ title: 'Could not set Metadata as Source!', color: 'error' });
    }
};
