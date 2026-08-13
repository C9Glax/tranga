import type { PatchMangasByMangaIdMonitoredResponses } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';

export const patchMangaMonitored = async (mangaId: string, monitored: boolean) => {
    const { $tranga } = useNuxtApp();
    const toast = useToast();
    try {
        await $tranga<PatchMangasByMangaIdMonitoredResponses>(`/mangas/${mangaId}/monitored`, { method: 'patch', body: { monitored } });
        await refreshNuxtData([ApiKeys.Manga.Manga(mangaId)]);
        toast.add({ title: monitored ? 'Now Monitoring' : 'Stopped Monitoring', color: 'success' });
    } catch {
        toast.add({ title: monitored ? 'Could not start monitoring' : 'Could not stop monitoring', color: 'error' });
    }
};
