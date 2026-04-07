<template>
    <TrangaPage>
        <UPageSection :ui="{ container: 'sm:py-0 lg:py-0 gap-8 sm:gap-8' }">
            <MetadataList :metadata-list="metadataSources" :loading="statusMetadata !== 'success'" :manga-id="mangaId" />
        </UPageSection>
    </TrangaPage>
</template>

<script setup lang="ts">
import type { GetMangasByMangaIdMetadataRelatedResponse } from '~/api/trangaApi';

const mangaId = useRoute().params.mangaId as string;

const { data: metadataSources, status: statusMetadata } = await useTranga<GetMangasByMangaIdMetadataRelatedResponse>(
    () => `/mangas/${mangaId}/metadata/related`,
    { key: ApiKeys.MangaMetadataEntries(mangaId) }
);
</script>
