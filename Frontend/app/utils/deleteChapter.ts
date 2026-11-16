import type { DeleteMangasChaptersByChapterIdResponses } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';

export const deleteChapter = async (mangaId: string, chapterId: string) => {
    const { $tranga, $i18n } = useNuxtApp();
    const toast = useToast();
    try {
        await $tranga<DeleteMangasChaptersByChapterIdResponses>(`/mangas/chapters/${chapterId}`, { method: 'delete' });
        toast.add({ title: $i18n.t('toast.chapterDeleted'), color: 'success' });
        await refreshNuxtData([ApiKeys.Manga.Chapters.List(mangaId)]);
    } catch {
        toast.add({ title: $i18n.t('toast.chapterDeleteFailed'), color: 'error' });
    }
};
