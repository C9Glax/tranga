<template>
    <MetadataPage :metadata="metadata" :actions="actions" :loading="statusMetadata !== 'success'" />
</template>

<script setup lang="ts">
import type { GetMetadataByMetadataIdResponse, Metadata } from '~/api/trangaApi';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';
import { patchMangaMetadataSource } from '~/utils/patchMangaMetadataSource';

const metadataId = useRoute().params.metadataId as string;
const mangaId = useRoute().query.mangaId as string | undefined;

const { data: metadata, status: statusMetadata } = await useTranga<GetMetadataByMetadataIdResponse>(() => `/metadata/${metadataId}`, {
    key: ApiKeys.Metadata(metadataId),
});

const actions = (m?: Metadata): ButtonProps[] | undefined => {
    const items: ButtonProps[] = [];

    if (metadata.value && mangaId && metadata.value.mangaIds.find((id) => id === mangaId)) {
        items.push({ label: 'Go to Manga', icon: 'i-lucide-book', to: `/manga/${mangaId}`, variant: 'soft' });
        items.push({
            label: metadata.value.chosen ? 'Is Source' : 'Use as Source for Manga',
            onClick: async () => await patchMangaMetadataSource(metadataId, mangaId),
            disabled: metadata.value?.chosen ?? false,
            variant: metadata.value.chosen ? 'outline' : 'solid',
        });
    }

    return items;
};
</script>
