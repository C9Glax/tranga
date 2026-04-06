<template>
    <MangaPage :manga="manga" :actions="actions" :loading="statusManga !== 'success'">
        <MetadataList
            :metadata-list="metadataSources"
            :loading="statusMetadata !== 'success'"
            :manga-id="mangaId"
            :actions="metadataActions" />
    </MangaPage>
</template>

<script setup lang="ts">
import type { GetMangasByMangaIdMetadataResponse, GetMangasByMangaIdResponse, Manga, Metadata } from '~/api/trangaApi';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';

const mangaId = useRoute().params.mangaId as string;

const { data: manga, status: statusManga } = await useTranga<GetMangasByMangaIdResponse>(() => `/mangas/${mangaId}`, {
    key: ApiKeys.Manga(mangaId),
});

const { data: metadataSources, status: statusMetadata } = await useTranga<GetMangasByMangaIdMetadataResponse>(
    () => `/mangas/${mangaId}/metadata`,
    { key: ApiKeys.MangaMetadata(mangaId), lazy: true }
);

const actions = (manga?: Manga): ButtonProps[] | undefined => [];

const metadataActions = (metadata: Metadata): ButtonProps[] => [
    {
        disabled: metadata.chosen ?? false,
        variant: metadata.chosen ? 'outline' : 'soft',
        label: metadata.chosen ? 'Is Source' : 'Use as Source',
        onClick: async () => (mangaId ? await patchMangaMetadataSource(metadata.metadataId, mangaId) : undefined),
    },
];
</script>
