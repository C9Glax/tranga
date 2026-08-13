<template>
    <UTable
        ref="tableRef"
        v-model:sorting="sorting"
        v-model:grouping="grouping"
        v-model:expanded="expanded"
        :grouping-options="groupingOptions"
        :data="sorted"
        :columns="columns"
        :loading="loading && !tasks"
        sticky
        class="w-full h-full">
        <template #type-cell="{ row }">
            <span v-if="row.getIsGrouped()" class="text-dimmed">-</span>
            <UTooltip v-else :text="`${row.original.taskType} · ${row.original.taskTypeId}`">
                <UButton
                    :to="`/tasks/${row.original.taskId}`"
                    :icon="row.original.taskType === 'PeriodicTask' ? 'i-lucide-repeat' : 'i-lucide-line-dot-right-horizontal'"
                    :label="taskTypeLabel(row.original.taskTypeName)"
                    variant="subtle"
                    color="neutral" />
            </UTooltip>
        </template>

        <template #state-cell="{ row }">
            <span v-if="row.getIsGrouped()" class="text-dimmed">-</span>
            <UTooltip v-else :text="taskStateDescription(row.original.status)">
                <UBadge :label="row.original.status" :color="taskStateBadgeColor(row.original.status)" variant="subtle" />
            </UTooltip>
        </template>

        <template #manga-cell="{ row }">
            <div v-if="row.getIsGrouped()" class="flex items-center gap-2">
                <UButton
                    :icon="row.getIsExpanded() ? 'i-lucide-chevron-down' : 'i-lucide-chevron-right'"
                    variant="ghost"
                    color="neutral"
                    size="xs"
                    @click="row.toggleExpanded()" />
                <TasksMangaCell :manga="(row.subRows[0]!.original as ServicesTasksTaskChapterTask).manga" />
                <UBadge :label="`${row.subRows.length} chapters`" variant="subtle" color="neutral" />
            </div>
            <TasksMangaCell v-else-if="'manga' in row.original" :manga="row.original.manga" />
        </template>

        <template #chapter-cell="{ row }">
            <span v-if="row.getIsGrouped()" class="text-dimmed">-</span>
            <TasksChapterCell v-else-if="'chapter' in row.original" :chapter="row.original.chapter" />
        </template>

        <template #lastRun-cell="{ row }">
            <span v-if="row.getIsGrouped()" class="text-dimmed">-</span>
            <template v-else>
                <TrangaTime v-if="row.original.lastRun" :model-value="row.original.lastRun" relative />
                <span v-else class="text-dimmed">-</span>
            </template>
        </template>

        <template #interval-cell="{ row }">
            <span v-if="row.getIsGrouped()" class="text-dimmed">-</span>
            <template v-else>
                <UBadge v-if="row.original.interval" :label="row.original.interval" variant="outline" color="neutral" />
                <span v-else class="text-dimmed">-</span>
            </template>
        </template>

        <template #nextRun-cell="{ row }">
            <span v-if="row.getIsGrouped()" class="text-dimmed">-</span>
            <template v-else>
                <TrangaTime v-if="nextRun(row.original)" :model-value="nextRun(row.original)" relative />
                <span v-else class="text-dimmed">-</span>
            </template>
        </template>
    </UTable>
</template>

<script setup lang="ts">
import type { ServicesTasksTask, ServicesTasksTaskChapterTask, ServicesTasksTaskState } from '~/api/tranga';
import type { TableColumn } from '@nuxt/ui/components/Table.vue';
import type { Column, ExpandedState, GroupingOptions, SortingState } from '@tanstack/vue-table';
import { getGroupedRowModel } from '@tanstack/vue-table';

const props = defineProps<{ tasks?: ServicesTasksTask[]; loading?: boolean }>();

const nextRun = (task: ServicesTasksTask): Date | undefined => {
    if (!task.lastRun || !task.interval) return undefined;
    const next = new Date(task.lastRun);
    const interval = parseTimespan(task.interval);
    if (!interval) return undefined;
    next.setTime(next.getTime() + interval);
    return next;
};

// Order is set server-side (newest TaskId first) so that batches stay stable for infinite scroll,
// unless the user opts into a column sort below.
const sorted = computed((): ServicesTasksTask[] => props.tasks ?? []);

const tableRef = useTemplateRef('tableRef');
defineExpose({ tableRef });

const sorting = ref<SortingState>([]);

// Group DownloadChapterTask rows for the same manga together; every other row gets a unique
// key (its own taskId) so it renders as a normal, ungrouped row.
const grouping = ref<string[]>(['manga']);
const groupingOptions = ref<GroupingOptions>({ groupedColumnMode: false, getGroupedRowModel: getGroupedRowModel() });
const expanded = ref<ExpandedState>(true);

const STATE_SORT_ORDER: Record<ServicesTasksTaskState, number> = { Failed: 0, Completed: 1, Running: 2, Queued: 3, Blocked: 4, Pending: 5 };

// Compares chapter numbers numerically when possible, falls back to string compare, nulls sort last.
const compareChapterNumber = (a: string | null, b: string | null): number => {
    if (a === b) return 0;
    if (a === null) return 1;
    if (b === null) return -1;

    const numberA = parseFloat(a);
    const numberB = parseFloat(b);
    if (!isNaN(numberA) && !isNaN(numberB)) return numberA - numberB;

    return a.localeCompare(b);
};

const chapterNumber = (task: ServicesTasksTask): string | null => ('chapter' in task ? task.chapter.number : null);

// Nulls/undefined sort last regardless of the current sort direction.
const compareNullableDate = (a: Date | string | null | undefined, b: Date | string | null | undefined): number => {
    if (!a && !b) return 0;
    if (!a) return 1;
    if (!b) return -1;
    return new Date(a).getTime() - new Date(b).getTime();
};

const sortableHeader = (label: string) => {
    return ({ column }: { column: Column<ServicesTasksTask, unknown> }) => {
        const isSorted = column.getIsSorted();
        return h(resolveComponent('UButton'), {
            color: 'neutral',
            variant: 'ghost',
            label,
            icon: isSorted
                ? isSorted === 'asc'
                    ? 'i-lucide-arrow-up-narrow-wide'
                    : 'i-lucide-arrow-down-wide-narrow'
                : 'i-lucide-arrow-up-down',
            class: '-mx-2.5',
            onClick: () => column.toggleSorting(isSorted === 'asc'),
        });
    };
};

const columns: TableColumn<ServicesTasksTask>[] = [
    { accessorKey: 'type', header: 'Type', enableSorting: false },
    {
        accessorKey: 'state',
        header: sortableHeader('State'),
        sortingFn: (rowA, rowB) => STATE_SORT_ORDER[rowA.original.status] - STATE_SORT_ORDER[rowB.original.status],
    },
    {
        id: 'manga',
        accessorFn: (task) => (task.taskTypeName === 'DownloadChapterTask' && 'manga' in task ? task.manga.mangaId : task.taskId),
        header: 'Manga',
        enableSorting: false,
    },
    {
        accessorKey: 'chapter',
        header: sortableHeader('Chapter'),
        sortingFn: (rowA, rowB) => compareChapterNumber(chapterNumber(rowA.original), chapterNumber(rowB.original)),
    },
    {
        accessorKey: 'lastRun',
        header: sortableHeader('Last Run'),
        sortingFn: (rowA, rowB) => compareNullableDate(rowA.original.lastRun, rowB.original.lastRun),
    },
    { accessorKey: 'interval', header: 'Interval', enableSorting: false },
    {
        accessorKey: 'nextRun',
        header: sortableHeader('Next Run'),
        sortingFn: (rowA, rowB) => compareNullableDate(nextRun(rowA.original), nextRun(rowB.original)),
    },
];
</script>
