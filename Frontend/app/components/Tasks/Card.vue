<template>
    <UCard>
        <div class="flex flex-col gap-4">
            <UTooltip :text="`${task.taskType} ${task.taskTypeId}`">
                <UUser
                    :name="splitCamelCase(task.taskTypeName)"
                    :description="task.taskId"
                    :avatar="{ icon: task.taskType === 'PeriodicTask' ? 'i-lucide-repeat' : 'i-lucide-line-dot-right-horizontal' }" />
            </UTooltip>
            <div class="flex gap-2 items-center">
                <TrangaTime v-model="task.lastRun" prefix="Last Run" />
                <UIcon v-if="task.interval" name="i-lucide-plus" />
                <TrangaDoubleBadge
                    v-if="task.interval"
                    :first-badge-props="{ label: 'Interval' }"
                    :second-badge-props="{ label: task.interval }" />
                <UIcon v-if="nextRun" name="i-lucide-arrow-right" />
                <TrangaTime v-if="nextRun" v-model="nextRun" prefix="Next Run" relative />
            </div>
        </div>

        <template #footer>
            <UFieldGroup>
                <UButton v-if="task.mangaId" :to="`/manga/${task.mangaId}`" label="Manga" variant="soft" />
                <UButton v-if="task.chapterId" :to="`/manga/${task.mangaId}`" label="Chapter" variant="soft" color="secondary" />
            </UFieldGroup>
        </template>
    </UCard>
</template>

<script setup lang="ts">
import type { ServicesTasksTask } from '~/api/tranga';

const props = defineProps<{ task: ServicesTasksTask }>();

const nextRun = computed((): Date | undefined => {
    if (!props.task.lastRun || !props.task.interval) return undefined;
    const nextRun = new Date(props.task.lastRun);
    const interval = parseTimespan(props.task.interval);
    if (!interval) return undefined;
    nextRun.setTime(nextRun.getTime() + interval);
    return nextRun;
});
</script>
