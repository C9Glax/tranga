import type { PostMangasByMangaIdMergeResponses, ServicesMangaPostMangaMergeRequest } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';

export const mergeManga = async (
    targetMangaId: string,
    sourceMangaId: string,
    keepSourceMetadata: boolean,
    keepSourceChapters: boolean,
) => {
    const { $tranga, $i18n } = useNuxtApp();
    const toast = useToast();
    try {
        const body: ServicesMangaPostMangaMergeRequest = { sourceMangaId, keepSourceMetadata, keepSourceChapters };
        await $tranga<PostMangasByMangaIdMergeResponses>(`/mangas/${targetMangaId}/merge`, { method: 'post', body });
        toast.add({ title: $i18n.t('toast.mangaMerged'), color: 'success' });
        await refreshNuxtData([ApiKeys.Manga.List]);
        await navigateTo(`/manga/${targetMangaId}`);
    } catch {
        toast.add({ title: $i18n.t('toast.mangaMergeFailed'), color: 'error' });
    }
};
