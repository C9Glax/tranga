import type { GetMangasDownloadLinksExtensionsResponse } from '~/api/tranga';

export default async function useDownloadExtensions() {
    const { data: extensions } = await useTranga<GetMangasDownloadLinksExtensionsResponse>('/mangas/downloadLinks/extensions', {
        key: ApiKeys.DownloadExtensions,
    });

    const downloadExtensions = computed(() => extensions.value?.extensions);

    return { downloadExtensions };
}
