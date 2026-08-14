import type { GetMangasMetadataExtensionsResponse, ServicesMangaMetadataExtension } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';

export default async function useMetadataExtensions() {
    const { data: extensions } = await useTranga<GetMangasMetadataExtensionsResponse>('/mangas/metadata/extensions', {
        key: ApiKeys.MetadataExtensions,
    });

    const metadataExtensions = computed(() => new MetadataExtensions(extensions.value?.extensions));

    return { metadataExtensions };
}

export class MetadataExtensions {
    constructor(extensions?: ServicesMangaMetadataExtension[]) {
        this.extensions = extensions;
    }

    extensions: ServicesMangaMetadataExtension[] | undefined;

    getExtension(id?: string): ServicesMangaMetadataExtension | null {
        return this.extensions?.find((e) => e.metadataExtensionId === id) ?? null;
    }
}
