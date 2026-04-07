<template>
    <TrangaPage :navigation-props="{ items: [{ label: 'To Manga', icon: 'i-lucide-book', to: `/manga/${mangaId}` }] }">
        <UPageSection :ui="{ container: 'sm:py-0 lg:py-0 gap-8 sm:gap-8' }" title="Search Result">
            <DownloadLinkList :download-links="downloadLinks" :loading="statusDownloadLinks !== 'success'" />
        </UPageSection>
    </TrangaPage>
</template>

<script setup lang="ts">
import type { PostMangasSearchByMangaIdDownloadLinksResponse } from '~/api/trangaApi';

const mangaId = useRoute().params.mangaId as string;

const { data: downloadLinks, status: statusDownloadLinks } = useTranga<PostMangasSearchByMangaIdDownloadLinksResponse>(
    () => `/mangas/search/${mangaId}/downloadLinks`,
    { method: 'POST' }
);
</script>
