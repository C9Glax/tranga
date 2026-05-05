<template>
    <TrangaPage
        :navigation-props="{ items: [{ label: 'To Manga', icon: 'i-lucide-book', to: `/manga/${mangaId}` }] }"
        :page-title="{ title: 'Manga Tasks', icon: { name: 'i-lucide-biceps-flexed' } }">
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
</script>
