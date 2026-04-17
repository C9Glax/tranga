import type { GetMangasMetadataExtensionsResponse } from '~/api/tranga';

export default async function useMetadataExtensions() {
    const { data: extensions } = await useTranga<GetMangasMetadataExtensionsResponse>('/mangas/metadata/extensions', {
        key: ApiKeys.MetadataExtensions,
    });

    const metadataExtensions = computed(() => extensions.value?.extensions);

    return { metadataExtensions };
}
