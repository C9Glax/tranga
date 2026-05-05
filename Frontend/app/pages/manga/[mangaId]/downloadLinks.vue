<template>
    <TrangaPage :navigation-props="navigation">
        <UPageSection :ui="{ container: 'sm:py-0 lg:py-0 gap-8 sm:gap-8' }" title="Search Result">
            <DownloadLinkList :download-links="downloadLinks" :loading="statusDownloadLinks !== 'success'" />
        </UPageSection>
    </TrangaPage>
</template>

<script setup lang="ts">
import type { PostMangasSearchByMangaIdDownloadLinksResponse } from '~/api/tranga';
import type { NavigationMenuProps } from '@nuxt/ui/components/NavigationMenu.vue';

const mangaId = useRoute().params.mangaId as string;

const { data: downloadLinks, status: statusDownloadLinks } = useTranga<PostMangasSearchByMangaIdDownloadLinksResponse>(
    () => `/mangas/search/${mangaId}/downloadLinks`,
    { method: 'POST' }
);

const navigation = computed((): NavigationMenuProps => {
    return {
        items: [
            { label: 'Manga', type: 'label' },
            { label: 'Manga', to: `/manga/${mangaId}`, icon: 'i-lucide-book' },
            { label: 'Metadata-Entries', to: `/manga/${mangaId}/metadataEntries`, icon: 'i-lucide-list' },
            { label: 'Manga Tasks', to: `/manga/${mangaId}/tasks`, icon: 'i-lucide-biceps-flexed' },
            { label: 'Manga Download Tasks', to: `/manga/${mangaId}/downloads`, icon: 'i-lucide-cloud-download' },
        ],
    };
});
</script>
