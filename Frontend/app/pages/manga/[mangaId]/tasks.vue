<template>
    <TrangaPage :navigation-props="navigation" :page-title="{ title: 'Manga Tasks', icon: { name: 'i-lucide-biceps-flexed' } }">
        <UPageSection :ui="{ container: 'sm:py-0 lg:py-0 gap-8 sm:gap-8 mb-8' }">
            <USwitch v-model="includeFinished" label="Include finished" />
        </UPageSection>
        <UPageSection :ui="{ container: 'sm:py-0 lg:py-0 gap-8 sm:gap-8' }">
            <TasksList :tasks="data" :loading="status !== 'success'" />
        </UPageSection>
    </TrangaPage>
</template>

<script setup lang="ts">
import type { GetTasksMangaByMangaIdResponse } from '~/api/tranga';
import type { NavigationMenuProps } from '@nuxt/ui/components/NavigationMenu.vue';

const mangaId = useRoute().params.mangaId as string;

const includeFinished = useState<boolean>(() => false);
const { data, refresh, status } = await useTranga<GetTasksMangaByMangaIdResponse>(
    () => `/tasks/manga/${mangaId}?includeFinished=${includeFinished.value}`,
    { lazy: true, watch: [includeFinished] }
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
            { label: 'Manga Tasks', to: `/manga/${mangaId}/tasks`, icon: 'i-lucide-biceps-flexed' },
            { label: 'Manga Download Tasks', to: `/manga/${mangaId}/downloads`, icon: 'i-lucide-cloud-download' },
        ],
    };
});
</script>
