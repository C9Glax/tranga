<template>
    <TrangaPage
        :navigation-props="navigation"
        :page-title="{ title: 'Manga Downloads', icon: { name: 'i-lucide-cloud-download' } }"
        rimless>
        <UPageSection :ui="{ container: 'px-0 max-w-none sm:py-0 lg:py-0 gap-8 sm:gap-8 mb-8' }">
            <USwitch v-model="includeFinished" label="Include finished downloads" />
        </UPageSection>
        <UPageSection :ui="{ container: 'py-0 sm:py-0 lg:py-0 px-0 sm:px-0 lg:px-0\' ' }">
            <TasksList :tasks="data" />
        </UPageSection>
    </TrangaPage>
</template>

<script setup lang="ts">
import type { GetTasksMangaByMangaIdDownloadsResponse } from '~/api/tranga';
import type { NavigationMenuProps } from '@nuxt/ui/components/NavigationMenu.vue';

const mangaId = useRoute().params.mangaId as string;

const includeFinished = useState<boolean>(() => false);
const { data, refresh } = await useTranga<GetTasksMangaByMangaIdDownloadsResponse>(
    () => `/tasks/manga/${mangaId}/downloads?includeFinished=${includeFinished.value}`,
    { lazy: true, watch: [includeFinished] },
);

defineShortcuts({ meta_r: () => refresh() });

let interval: number;
onMounted(() => {
    interval = setInterval(() => refresh(), 5000);
});
onUnmounted(() => clearInterval(interval));

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
