import type { PostMangasByMangaIdRemoveResponses } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';

export const removeManga = async (mangaId: string) => {
    const { $tranga, $i18n } = useNuxtApp();
    const toast = useToast();
    try {
        await $tranga<PostMangasByMangaIdRemoveResponses>(`/mangas/${mangaId}/remove`, { method: 'post' });
        await refreshNuxtData([ApiKeys.Manga.List]);
        toast.add({ title: $i18n.t('toast.mangaRemoved'), color: 'success' });
        await navigateTo('/');
    } catch {
        toast.add({ title: $i18n.t('toast.mangaRemoveFailed'), color: 'error' });
    }
};
