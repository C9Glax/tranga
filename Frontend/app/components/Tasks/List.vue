<template>
    <UPageList class="gap-4">
        <TasksCard v-if="tasks" v-for="task in sorted" :key="task.taskId" :task="task" />
        <USkeleton v-else class="h-48" />
    </UPageList>
</template>

<script setup lang="ts">
import type { ServicesTasksTask } from '~/api/tranga';

const props = defineProps<{ tasks?: ServicesTasksTask[] }>();

const sorted = computed((): ServicesTasksTask[] =>
    (props.tasks ?? []).sort((t1, t2) => {
        const t1Next = nextRun(t1);
        const t2Next = nextRun(t2);
        if (t1Next && t2Next) return t1Next < t2Next ? -1 : 1;
        else if (t1Next) return -1;
        else if (t2Next) return 1;
        else return 0;
    })
);

const nextRun = (task: ServicesTasksTask): Date | undefined => {
    if (!task.lastRun || !task.interval) return undefined;
    const nextRun = new Date(task.lastRun);
    const interval = parseTimespan(task.interval);
    if (!interval) return undefined;
    nextRun.setTime(nextRun.getTime() + interval);
    return nextRun;
};
</script>
