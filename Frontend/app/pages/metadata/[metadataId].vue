<template>
    <MetadataPage :metadata="metadata" :actions="actions" :loading="statusMetadata !== 'success'"> </MetadataPage>
</template>

<script setup lang="ts">
import type {
    GetMetadataByMetadataIdMangaRelatedResponse,
    GetMetadataByMetadataIdMangaResponse,
    GetMetadataByMetadataIdResponse,
    Metadata,
} from '~/api/trangaApi';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';
import { patchMangaMetadataSource } from '~/utils/patchMangaMetadataSource';

const metadataId = useRoute().params.metadataId as string;
const mangaId = useRoute().query.mangaId as string | undefined;

const { data: metadata, status: statusMetadata } = await useTranga<GetMetadataByMetadataIdResponse>(() => `/metadata/${metadataId}`, {
    key: ApiKeys.Metadata(metadataId),
});

const { data: manga } = await useTranga<GetMetadataByMetadataIdMangaResponse>(() => `/metadata/${metadataId}/manga`, {
    key: ApiKeys.MetadataManga(metadataId),
});

const { data: relatedMangaIds } = await useTranga<GetMetadataByMetadataIdMangaRelatedResponse>(
    () => `/metadata/${metadataId}/manga/related`,
    { key: ApiKeys.MetadataRelatedMangas(metadataId) }
);

const actions = (m?: Metadata): ButtonProps[] | undefined => {
    const items: ButtonProps[] = [];

    if (mangaId && relatedMangaIds.value?.find((m) => m === mangaId)) {
        items.push({
            label: 'Use as Source for Manga',
            onClick: async () => await patchMangaMetadataSource(metadataId, mangaId),
            variant: 'solid',
        });
    } else if (manga.value) {
        items.push({ label: 'Go to Manga', icon: 'i-lucide-book', to: `/manga/${manga.value.mangaId}`, variant: 'soft' });
        items.push({ label: 'Is Source', disabled: true, variant: 'outline' });
    } else if (relatedMangaIds.value?.length === 1) {
        items.push({
            label: 'Use as Source for Manga',
            onClick: async () => await patchMangaMetadataSource(metadataId, relatedMangaIds.value![0]!),
            variant: 'solid',
        });
    }

    return items;
};
</script>
