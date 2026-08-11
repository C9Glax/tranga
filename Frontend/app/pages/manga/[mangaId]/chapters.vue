<template>
    <TrangaPage :navigation="navigation" rimless>
        <ChaptersList :chapters="data" :loading="status !== 'success'" />
    </TrangaPage>
</template>

<script setup lang="ts">
import type { GetMangasByMangaIdChaptersResponse } from '~/api/tranga';
import type { NavigationMenuProps } from '@nuxt/ui/components/NavigationMenu.vue';
import { ApiKeys } from '~/composables/ApiKeys';
import type { TrangaPageTitleProps } from '~/components/Tranga/Page.vue';

const mangaId = useRoute().params.mangaId as string;

const { data, status } = await useTranga<GetMangasByMangaIdChaptersResponse>(() => `/mangas/${mangaId}/chapters`, {
    key: ApiKeys.Manga.Chapters.List(mangaId),
});

const navigation = computed((): TrangaPageTitleProps => {
    return {
        title: { label: 'Manga', type: 'label' },
        items: [
            { label: 'Manga', to: `/manga/${mangaId}`, icon: 'i-lucide-book' },
            { label: 'Metadata-Entries', to: `/manga/${mangaId}/metadataEntries`, icon: 'i-lucide-list' },
            { label: 'Manga Tasks', to: `/tasks?manga=${mangaId}`, icon: 'i-lucide-biceps-flexed' },
            { label: 'Manga Download Tasks', to: `/manga/${mangaId}/downloads`, icon: 'i-lucide-cloud-download' },
            { label: 'Chapters', to: `/manga/${mangaId}/chapters`, icon: 'i-lucide-list-checks' },
        ],
    };
});
</script>
