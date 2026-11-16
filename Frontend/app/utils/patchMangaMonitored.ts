import type { PatchMangasByMangaIdMonitoredResponses } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';

export const patchMangaMonitored = async (mangaId: string, monitored: boolean) => {
    const { $tranga, $i18n } = useNuxtApp();
    const toast = useToast();
    try {
        await $tranga<PatchMangasByMangaIdMonitoredResponses>(`/mangas/${mangaId}/monitored`, { method: 'patch', body: { monitored } });
        await refreshNuxtData([ApiKeys.Manga.Manga(mangaId), ApiKeys.Manga.List]);
        toast.add({ title: monitored ? $i18n.t('toast.monitoringStarted') : $i18n.t('toast.monitoringStopped'), color: 'success' });
    } catch {
        toast.add({ title: monitored ? $i18n.t('toast.monitoringStartFailed') : $i18n.t('toast.monitoringStopFailed'), color: 'error' });
    }
};
