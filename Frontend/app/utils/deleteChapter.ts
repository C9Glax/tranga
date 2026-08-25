import type { DeleteMangasChaptersByChapterIdResponses } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';

export const deleteChapter = async (mangaId: string, chapterId: string) => {
    const { $tranga } = useNuxtApp();
    const toast = useToast();
    try {
        await $tranga<DeleteMangasChaptersByChapterIdResponses>(`/mangas/chapters/${chapterId}`, { method: 'delete' });
        toast.add({ title: 'Chapter deleted', color: 'success' });
        await refreshNuxtData([ApiKeys.Manga.Chapters.List(mangaId)]);
    } catch {
        toast.add({ title: 'Could not delete Chapter!', color: 'error' });
    }
};
