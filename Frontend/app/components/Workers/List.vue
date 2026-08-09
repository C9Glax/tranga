<template>
    <UTable :data="sorted" :columns="columns" :loading="loading && !workers" class="w-full">
        <template #workerId-cell="{ row }">
            <UTooltip :text="row.original.workerId">
                <UBadge :label="row.original.workerId.slice(0, 8)" variant="subtle" color="neutral" />
            </UTooltip>
        </template>

        <template #status-cell="{ row }">
            <UBadge :label="row.original.status" :color="workerStatusBadgeColor(row.original.status)" variant="subtle" />
        </template>

        <template #currentTask-cell="{ row }">
            <div v-if="row.original.currentTaskId" class="flex gap-1 items-center flex-wrap">
                <UTooltip :text="row.original.currentTaskId">
                    <UBadge
                        :icon="currentTask(row.original)?.taskType === 'PeriodicTask' ? 'i-lucide-repeat' : 'i-lucide-line-dot-right-horizontal'"
                        :label="currentTask(row.original) ? taskTypeLabel(currentTask(row.original)!.taskTypeName) : 'Unknown task'"
                        variant="subtle"
                        color="neutral" />
                </UTooltip>
                <TasksMangaCell v-if="currentTaskManga(row.original)" :manga="currentTaskManga(row.original)!" />
                <TasksChapterCell v-if="currentTaskChapter(row.original)" :chapter="currentTaskChapter(row.original)!" />
            </div>
            <span v-else class="text-dimmed">-</span>
        </template>

        <template #startedAt-cell="{ row }">
            <TrangaTime :model-value="row.original.startedAt" relative />
        </template>

        <template #lastHeartbeat-cell="{ row }">
            <TrangaTime :model-value="row.original.lastHeartbeat" relative />
        </template>
    </UTable>
</template>

<script setup lang="ts">
import type { ServicesTasksWorker, ServicesTasksTask } from '~/api/tranga';
import type { TableColumn } from '@nuxt/ui/components/Table.vue';

const props = defineProps<{ workers?: ServicesTasksWorker[]; tasks?: ServicesTasksTask[]; loading?: boolean }>();

const currentTask = (worker: ServicesTasksWorker): ServicesTasksTask | undefined =>
    (props.tasks ?? []).find((t) => t.taskId === worker.currentTaskId);

const currentTaskManga = (worker: ServicesTasksWorker) => {
    const task = currentTask(worker);
    return task && 'manga' in task ? task.manga : undefined;
};

const currentTaskChapter = (worker: ServicesTasksWorker) => {
    const task = currentTask(worker);
    return task && 'chapter' in task ? task.chapter : undefined;
};

const statusOrder: Record<ServicesTasksWorker['status'], number> = { Busy: 0, Retiring: 1, Idle: 2 };

const sorted = computed((): ServicesTasksWorker[] =>
    [...(props.workers ?? [])].sort((w1, w2) => statusOrder[w1.status] - statusOrder[w2.status])
);

const columns: TableColumn<ServicesTasksWorker>[] = [
    { accessorKey: 'workerId', header: 'Worker' },
    { accessorKey: 'status', header: 'Status' },
    { accessorKey: 'currentTask', header: 'Current Task' },
    { accessorKey: 'startedAt', header: 'Started At' },
    { accessorKey: 'lastHeartbeat', header: 'Last Heartbeat' },
];
</script>
