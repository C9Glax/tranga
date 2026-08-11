import type { GetMangasMetadataExtensionsResponse, ServicesMangaIMetadataExtension } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';

export default async function useMetadataExtensions() {
    const { data: extensions } = await useTranga<GetMangasMetadataExtensionsResponse>('/mangas/metadata/extensions', {
        key: ApiKeys.MetadataExtensions,
    });

    const metadataExtensions = computed(() => new MetadataExtensions(extensions.value?.extensions));

    return { metadataExtensions };
}

export class MetadataExtensions {
    constructor(extensions?: ServicesMangaIMetadataExtension[]) {
        this.extensions = extensions;
    }

    extensions: ServicesMangaIMetadataExtension[] | undefined;

    getExtension(id?: string) : ServicesMangaIMetadataExtension | null {
        return this.extensions?.find(e => e.metadataExtensionId === id) ?? null;
    }
}
