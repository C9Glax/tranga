import type { GetDownloadLinksExtensionsResponse } from '~/api/trangaApi';

export default async function useDownloadExtensions() {
    const { data: extensions } = await useTranga<GetDownloadLinksExtensionsResponse>('/downloadLinks/extensions', {
        key: ApiKeys.DownloadExtensions,
    });

    const downloadExtensions = computed(() => extensions.value?.extensions);

    return { downloadExtensions };
}
