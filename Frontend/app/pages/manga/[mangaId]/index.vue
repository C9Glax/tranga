<template>
    <MangaPage :manga="manga" :actions="actions" :loading="statusManga !== 'success'">
        <MetadataList :metadata-list="metadataSources" :loading="statusMetadata !== 'success'" :manga-id="mangaId" />
    </MangaPage>
</template>

<script setup lang="ts">
import type {
    GetMangasByMangaIdMetadataRelatedResponse,
    GetMangasByMangaIdMetadataResponse,
    GetMangasByMangaIdResponse,
    Manga,
    Metadata,
} from '~/api/trangaApi';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';

const mangaId = useRoute().params.mangaId as string;

const { data: manga, status: statusManga } = await useTranga<GetMangasByMangaIdResponse>(() => `/mangas/${mangaId}`, {
    key: ApiKeys.Manga(mangaId),
});

const { data: metadataSources, status: statusMetadata } = await useTranga<GetMangasByMangaIdMetadataRelatedResponse>(
    () => `/mangas/${mangaId}/metadata/related`,
    { key: ApiKeys.MangaMetadataEntries(mangaId) }
);

const actions = (manga?: Manga): ButtonProps[] | undefined => [];
</script>
