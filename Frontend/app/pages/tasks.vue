<template>
    <TrangaPage :page-title="{ title: 'All Tasks', icon: { name: 'i-lucide-biceps-flexed' } }">
        <UPageSection :ui="{ container: 'sm:py-0 lg:py-0 gap-8 sm:gap-8 mb-8' }">
            <USwitch v-model="includeFinished" label="Include finished" />
        </UPageSection>
        <UPageSection :ui="{ container: 'sm:py-0 lg:py-0 gap-8 sm:gap-8' }">
            <TasksList :tasks="data" />
        </UPageSection>
    </TrangaPage>
</template>

<script setup lang="ts">
import type { GetTasksResponse } from '~/api/tranga';
import { setInterval } from '#imports';

const includeFinished = useState<boolean>(() => false);
const { data, refresh } = await useTranga<GetTasksResponse>(() => `/tasks?includeFinished=${includeFinished.value}`, {
    lazy: true,
    watch: [includeFinished],
});

defineShortcuts({ meta_r: () => refresh() });

let interval: number;
onMounted(() => {
    interval = setInterval(() => refresh(), 5000);
});
onUnmounted(() => clearInterval(interval));
</script>
