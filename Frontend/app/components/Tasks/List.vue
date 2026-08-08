<template>
    <UTable :data="sorted" :columns="columns" :loading="loading && !tasks" class="w-full">
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
            <TasksMangaCell v-if="row.original.mangaId" :manga-id="row.original.mangaId" />
        </template>

        <template #chapter-cell="{ row }">
            <TasksChapterCell v-if="row.original.chapterId" :chapter-id="row.original.chapterId" />
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

const sorted = computed((): ServicesTasksTask[] =>
    [...(props.tasks ?? [])].sort((t1, t2) => {
        const t1Next = nextRun(t1);
        const t2Next = nextRun(t2);
        if (t1Next && t2Next) return t1Next < t2Next ? -1 : 1;
        else if (t1Next) return -1;
        else if (t2Next) return 1;
        else return 0;
    })
);

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
