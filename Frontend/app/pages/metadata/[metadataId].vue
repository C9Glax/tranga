<template>
    <MetadataPage :metadata="metadata" :actions="actions" :loading="statusMetadata !== 'success'"> </MetadataPage>
</template>

<script setup lang="ts">
import type { GetMetadataByMetadataIdResponse, MangaMetadata, PatchMangasByMangaIdUseMetadataResponses } from '~/api/trangaApi';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';
import { patchMangaMetadataSource } from '~/utils/patchMangaMetadataSource';

const metadataId = useRoute().params.metadataId as string;

const { data: metadata, status: statusMetadata } = await useTranga<GetMetadataByMetadataIdResponse>(() => `/metadata/${metadataId}`, {
    key: ApiKeys.Metadata(metadataId),
});

const actions = (m?: MangaMetadata): ButtonProps[] | undefined => {
    const items: ButtonProps[] = [];

    if (metadata.value && metadata.value.mangaIds.length == 1) {
        items.push({
            label: metadata.value.chosen ? 'Is Source' : 'Use as Source for Manga',
            onClick: async () => await patchMangaMetadataSource(metadataId, metadata.value!.mangaIds[0]!),
            disabled: metadata.value?.chosen ?? false,
            variant: metadata.value.chosen ? 'outline' : 'solid',
        });
    }

    return items;
};
</script>
