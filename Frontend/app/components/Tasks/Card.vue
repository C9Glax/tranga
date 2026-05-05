<template>
    <UPageCard>
        <template #header>
            <UTooltip :text="task.taskId">
                {{ splitCamelCase(task.taskTypeName) }}
            </UTooltip>
        </template>
        <template #footer>
            <div>
                <UButton v-if="task.mangaId" :to="`/manga/${task.mangaId}`" label="Manga" />
                <UButton v-if="task.mangaId" :to="`/manga/${task.mangaId}`" label="Chapter" />
            </div>
        </template>
        <TrangaDoubleBadge v-if="task.interval" :first-badge-props="{ label: 'Interval' }" :second-badge-props="{ label: task.interval }" />
        <TrangaTime v-if="task.lastRun" v-model="task.lastRun" prefix="Last Run" variant="outline" />
        <TrangaTime v-if="nextRun" v-model="nextRun" prefix="Next Run" variant="outline" />
    </UPageCard>
</template>

<script setup lang="ts">
import type { ServicesTasksTask } from '~/api/tranga';
import { splitCamelCase } from '~/utils/splitCamelCase';

const props = defineProps<{ task: ServicesTasksTask }>();

const nextRun = computed((): Date | undefined => {
    if (!props.task.lastRun || !props.task.interval) return undefined;
    const nextRun = new Date(props.task.lastRun);
    const interval = parseTimespan(props.task.interval);
    if (!interval) return undefined;
    console.log(nextRun, interval);
    nextRun.setTime(nextRun.getTime() + interval);
    return nextRun;
});
</script>
