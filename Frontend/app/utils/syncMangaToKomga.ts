import type { PostMangasByMangaIdSyncResponses } from '~/api/tranga';

export const syncMangaToKomga = async (mangaId: string) => {
    const { $tranga } = useNuxtApp();
    const toast = useToast();
    try {
        await $tranga<PostMangasByMangaIdSyncResponses>(`/mangas/${mangaId}/sync`, { method: 'post' });
        toast.add({ title: 'Komga sync queued!', color: 'success' });
    } catch {
        toast.add({ title: 'Could not queue Komga sync!', color: 'error' });
    }
};
