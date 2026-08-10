<template>
    <TrangaPage :navigation-props="navigation" :page-title="{ title: 'Download-Links', icon: { name: 'i-lucide-download' } }">
        <UPageSection :ui="{ container: 'sm:py-0 lg:py-0 gap-8 sm:gap-8' }" title="Search Result">
            <UInput
                v-model="searchTerm"
                placeholder="Search term (leave blank to use the manga's stored title)"
                :loading="statusDownloadLinks === 'pending'"
                :disabled="statusDownloadLinks === 'pending'"
                class="w-full"
                @keyup.enter="refreshDownloadLinks">
                <template #trailing>
                    <UIcon class="cursor-pointer" name="i-lucide-search" @click="refreshDownloadLinks" />
                </template>
            </UInput>
            <DownloadLinkList :download-links="downloadLinks" :loading="statusDownloadLinks !== 'success'" />
        </UPageSection>
    </TrangaPage>
</template>

<script setup lang="ts">
import type { PostMangasSearchByMangaIdDownloadLinksResponse } from '~/api/tranga';
import type { NavigationMenuProps } from '@nuxt/ui/components/NavigationMenu.vue';

const mangaId = useRoute().params.mangaId as string;

const searchTerm = ref<string>('');

const {
    data: downloadLinks,
    status: statusDownloadLinks,
    refresh: refreshDownloadLinks,
} = useTranga<PostMangasSearchByMangaIdDownloadLinksResponse>(() => `/mangas/search/${mangaId}/downloadLinks`, {
    method: 'POST',
    body: computed(() => (searchTerm.value ? { searchTerm: searchTerm.value } : undefined)),
    watch: false,
});

const navigation = computed((): NavigationMenuProps => {
    return {
        items: [
            { label: 'Manga', type: 'label' },
            { label: 'Manga', to: `/manga/${mangaId}`, icon: 'i-lucide-book' },
            { label: 'Metadata-Entries', to: `/manga/${mangaId}/metadataEntries`, icon: 'i-lucide-list' },
            { label: 'Manga Tasks', to: `/tasks?manga=${mangaId}`, icon: 'i-lucide-biceps-flexed' },
            { label: 'Manga Download Tasks', to: `/manga/${mangaId}/downloads`, icon: 'i-lucide-cloud-download' },
            { label: 'Chapters', to: `/manga/${mangaId}/chapters`, icon: 'i-lucide-list-checks' },
        ],
    };
});
</script>
