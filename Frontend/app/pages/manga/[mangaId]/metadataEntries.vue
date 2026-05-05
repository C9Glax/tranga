<template>
    <TrangaPage :navigation-props="navigation" :page-title="{ title: 'Metadata-Entries', icon: { name: 'i-lucide-list' } }">
        <UPageSection :ui="{ container: 'sm:py-0 lg:py-0 gap-8 sm:gap-8' }">
            <MetadataList :metadata-list="metadataSources" :loading="statusMetadata !== 'success'" :manga-id="mangaId" />
        </UPageSection>
    </TrangaPage>
</template>

<script setup lang="ts">
import type { GetMangasByMangaIdMetadataRelatedResponse } from '~/api/tranga';
import type { NavigationMenuProps } from '@nuxt/ui/components/NavigationMenu.vue';

const mangaId = useRoute().params.mangaId as string;

const { data: metadataSources, status: statusMetadata } = await useTranga<GetMangasByMangaIdMetadataRelatedResponse>(
    () => `/mangas/${mangaId}/metadata/related`,
    { key: ApiKeys.MangaMetadataEntries(mangaId) }
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
