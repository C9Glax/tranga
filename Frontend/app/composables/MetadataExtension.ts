import type { GetMetadataExtensionsResponse } from '~/api/trangaApi';

export default async function useMetadataExtensions() {
    const { data: extensions } = await useTranga<GetMetadataExtensionsResponse>('/metadata/extensions', {
        key: ApiKeys.MetadataExtensions,
    });

    const metadataExtensions = computed(() => extensions.value?.extensions);

    return { metadataExtensions };
}
