<template>
    <UTable ref="tableRef" :data="sorted" :columns="columns" :loading="loading && !tasks" sticky class="w-full h-[70vh]">
        <template #type-cell="{ row }">
            <UTooltip :text="`${row.original.taskType} · ${row.original.taskTypeId}`">
                <UBadge
                    :icon="row.original.taskType === 'PeriodicTask' ? 'i-lucide-repeat' : 'i-lucide-line-dot-right-horizontal'"
                    :label="taskTypeLabel(row.original.taskTypeName)"
                    variant="subtle"
                    color="neutral" />
            </UTooltip>
        </template>

        <template #state-cell="{ row }">
            <UTooltip :text="taskStateDescription(row.original.status)">
                <UBadge :label="row.original.status" :color="taskStateBadgeColor(row.original.status)" variant="subtle" />
            </UTooltip>
        </template>

        <template #manga-cell="{ row }">
            <TasksMangaCell v-if="'manga' in row.original" :manga="row.original.manga" />
        </template>

        <template #chapter-cell="{ row }">
            <TasksChapterCell v-if="'chapter' in row.original" :chapter="row.original.chapter" />
        </template>

        <template #lastRun-cell="{ row }">
            <TrangaTime v-if="row.original.lastRun" :model-value="row.original.lastRun" relative />
            <span v-else class="text-dimmed">-</span>
        </template>

        <template #interval-cell="{ row }">
            <UBadge v-if="row.original.interval" :label="row.original.interval" variant="outline" color="neutral" />
            <span v-else class="text-dimmed">-</span>
        </template>

        <template #nextRun-cell="{ row }">
            <TrangaTime v-if="nextRun(row.original)" :model-value="nextRun(row.original)" relative />
            <span v-else class="text-dimmed">-</span>
        </template>
    </UTable>
</template>

<script setup lang="ts">
import type { ServicesTasksTask } from '~/api/tranga';
import type { TableColumn } from '@nuxt/ui/components/Table.vue';

const props = defineProps<{ tasks?: ServicesTasksTask[]; loading?: boolean }>();

const nextRun = (task: ServicesTasksTask): Date | undefined => {
    if (!task.lastRun || !task.interval) return undefined;
    const next = new Date(task.lastRun);
    const interval = parseTimespan(task.interval);
    if (!interval) return undefined;
    next.setTime(next.getTime() + interval);
    return next;
};

// Order is set server-side (newest TaskId first) so that batches stay stable for infinite scroll.
const sorted = computed((): ServicesTasksTask[] => props.tasks ?? []);

const tableRef = useTemplateRef('tableRef');
defineExpose({ tableRef });

const columns: TableColumn<ServicesTasksTask>[] = [
    { accessorKey: 'type', header: 'Type' },
    { accessorKey: 'state', header: 'State' },
    { accessorKey: 'manga', header: 'Manga' },
    { accessorKey: 'chapter', header: 'Chapter' },
    { accessorKey: 'lastRun', header: 'Last Run' },
    { accessorKey: 'interval', header: 'Interval' },
    { accessorKey: 'nextRun', header: 'Next Run' },
];
</script>
