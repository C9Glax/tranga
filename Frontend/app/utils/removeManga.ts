import type { PostMangasByMangaIdRemoveResponses } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';

export const removeManga = async (mangaId: string) => {
    const { $tranga } = useNuxtApp();
    const toast = useToast();
    try {
        await $tranga<PostMangasByMangaIdRemoveResponses>(`/mangas/${mangaId}/remove`, { method: 'post' });
        await refreshNuxtData([ApiKeys.Manga.List]);
        toast.add({ title: 'Manga removed', color: 'success' });
        await navigateTo('/');
    } catch {
        toast.add({ title: 'Could not remove Manga!', color: 'error' });
    }
};
