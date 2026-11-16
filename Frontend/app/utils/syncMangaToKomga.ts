import type { PostMangasByMangaIdSyncResponses } from '~/api/tranga';

export const syncMangaToKomga = async (mangaId: string) => {
    const { $tranga, $i18n } = useNuxtApp();
    const toast = useToast();
    try {
        await $tranga<PostMangasByMangaIdSyncResponses>(`/mangas/${mangaId}/sync`, { method: 'post' });
        toast.add({ title: $i18n.t('toast.syncQueued'), color: 'success' });
    } catch {
        toast.add({ title: $i18n.t('toast.syncFailed'), color: 'error' });
    }
};
