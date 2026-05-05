<template>
    <TrangaPage :page-title="{ title: 'Active Downloads', icon: { name: 'i-lucide-cloud-download' } }">
        <UPageSection :ui="{ container: 'sm:py-0 lg:py-0 gap-8 sm:gap-8 mb-8' }">
            <USwitch v-model="includeFinished" label="Include finished downloads" />
        </UPageSection>
        <UPageSection :ui="{ container: 'sm:py-0 lg:py-0 gap-8 sm:gap-8' }">
            <TasksList :tasks="data" :loading="status !== 'success'" />
        </UPageSection>
    </TrangaPage>
</template>

<script setup lang="ts">
import type { GetTasksMangaDownloadsResponse } from '~/api/tranga';

const includeFinished = useState<boolean>(() => false);
const { data, refresh, status } = await useTranga<GetTasksMangaDownloadsResponse>(
    () => `/tasks/manga/downloads?includeFinished=${includeFinished.value}`,
    { lazy: true, watch: [includeFinished] }
);

defineShortcuts({ meta_r: () => refresh() });

let interval: number;
onMounted(() => {
    interval = setInterval(() => refresh(), 5000);
});
onUnmounted(() => clearInterval(interval));
</script>
