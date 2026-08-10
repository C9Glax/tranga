import type { ServicesMangaPatchMangaMonitoredRequest, PatchMangasByMangaIdMonitoredResponses } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';

export const patchMangaMonitored = async (mangaId: string, monitored: boolean) => {
    const { $tranga } = useNuxtApp();
    const toast = useToast();
    try {
        await $tranga<PatchMangasByMangaIdMonitoredResponses>(`/mangas/${mangaId}/monitored`, {
            method: 'patch',
            body: { monitored } satisfies ServicesMangaPatchMangaMonitoredRequest,
        });
        await refreshNuxtData(ApiKeys.Manga.Manga(mangaId));
        toast.add({ title: monitored ? 'Manga added back to monitoring' : 'Manga removed from monitoring', color: 'success' });
    } catch {
        toast.add({
            title: monitored ? 'Could not re-add Manga to monitoring!' : 'Could not remove Manga from monitoring!',
            color: 'error',
        });
    }
};
