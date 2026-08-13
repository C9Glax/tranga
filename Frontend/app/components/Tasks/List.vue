<template>
    <UTable
        ref="tableRef"
        v-model:sorting="sorting"
        :data="displayRows"
        :columns="columns"
        :loading="loading && !tasks"
        :meta="rowMeta"
        sticky
        class="w-full h-full">
        <template #expand-cell="{ row }">
            <UButton
                v-if="isGroupRow(row.original)"
                :icon="expandedGroups.has(row.original.mangaId) ? 'i-lucide-chevron-down' : 'i-lucide-chevron-right'"
                variant="ghost"
                color="neutral"
                size="xs"
                @click="toggleGroup(row.original.mangaId)" />
        </template>

        <template #type-cell="{ row }">
            <UBadge
                v-if="isGroupRow(row.original)"
                :label="taskTypeLabel(row.original.tasks[0]!.taskTypeName)"
                icon="i-lucide-line-dot-right-horizontal"
                variant="subtle"
                color="neutral" />
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
            <span v-if="isGroupRow(row.original)" class="text-dimmed">-</span>
            <UTooltip v-else :text="taskStateDescription(row.original.status)">
                <UBadge :label="row.original.status" :color="taskStateBadgeColor(row.original.status)" variant="subtle" />
            </UTooltip>
        </template>

        <template #manga-cell="{ row }">
            <TasksMangaCell v-if="isGroupRow(row.original)" :manga="row.original.manga" />
            <TasksMangaCell v-else-if="'manga' in row.original" :manga="row.original.manga" />
        </template>

        <template #chapter-cell="{ row }">
            <UBadge v-if="isGroupRow(row.original)" :label="`${row.original.tasks.length} chapters`" variant="subtle" color="neutral" />
            <TasksChapterCell v-else-if="'chapter' in row.original" :chapter="row.original.chapter" />
        </template>

        <template #lastRun-cell="{ row }">
            <span v-if="isGroupRow(row.original)" class="text-dimmed">-</span>
            <template v-else>
                <TrangaTime v-if="row.original.lastRun" :model-value="row.original.lastRun" relative />
                <span v-else class="text-dimmed">-</span>
            </template>
        </template>

        <template #interval-cell="{ row }">
            <span v-if="isGroupRow(row.original)" class="text-dimmed">-</span>
            <template v-else>
                <UBadge v-if="row.original.interval" :label="row.original.interval" variant="outline" color="neutral" />
                <span v-else class="text-dimmed">-</span>
            </template>
        </template>

        <template #nextRun-cell="{ row }">
            <span v-if="isGroupRow(row.original)" class="text-dimmed">-</span>
            <template v-else>
                <TrangaTime v-if="nextRun(row.original)" :model-value="nextRun(row.original)" relative />
                <span v-else class="text-dimmed">-</span>
            </template>
        </template>
    </UTable>
</template>

<script setup lang="ts">
import type { ServicesTasksChapterSummary, ServicesTasksMangaSummary, ServicesTasksTask, ServicesTasksTaskState } from '~/api/tranga';
import type { TableColumn } from '@nuxt/ui/components/Table.vue';
import type { Column, Row, SortingState } from '@tanstack/vue-table';

// Multiple DownloadChapterTask rows for the same manga are collapsed into a single group row
// (see #manga-cell); every other task renders as a normal row. Grouping is done as a plain data
// transform rather than via TanStack's built-in grouping row model, since that model always
// nests every row (even singleton groups) under a synthetic parent row - which would wrap every
// non-chapter-download task in a spurious, duplicate "group" row too.
type ChapterTask = Extract<ServicesTasksTask, { chapter: ServicesTasksChapterSummary }>;
type ChapterTaskGroup = { __group: true; mangaId: string; manga: ServicesTasksMangaSummary; tasks: ChapterTask[] };
type DisplayRow = ServicesTasksTask | ChapterTaskGroup;

const isGroupRow = (row: DisplayRow): row is ChapterTaskGroup => '__group' in row;

const props = defineProps<{ tasks?: ServicesTasksTask[]; loading?: boolean }>();

const nextRun = (task: ServicesTasksTask): Date | undefined => {
    if (!task.lastRun || !task.interval) return undefined;
    const next = new Date(task.lastRun);
    const interval = parseTimespan(task.interval);
    if (!interval) return undefined;
    next.setTime(next.getTime() + interval);
    return next;
};

const expandedGroups = ref<Set<string>>(new Set());
const toggleGroup = (mangaId: string) => {
    const next = new Set(expandedGroups.value);
    if (next.has(mangaId)) next.delete(mangaId);
    else next.add(mangaId);
    expandedGroups.value = next;
};

// Order is set server-side (newest TaskId first) so that batches stay stable for infinite scroll,
// unless the user opts into a column sort below.
const displayRowsResult = computed((): { rows: DisplayRow[]; childTaskIds: Set<string> } => {
    const tasks = props.tasks ?? [];

    const chapterTasksByManga = new Map<string, ChapterTask[]>();
    for (const task of tasks) {
        if (task.taskTypeName === 'DownloadChapterTask' && 'chapter' in task) {
            const list = chapterTasksByManga.get(task.manga.mangaId) ?? [];
            list.push(task);
            chapterTasksByManga.set(task.manga.mangaId, list);
        }
    }

    const rows: DisplayRow[] = [];
    const childTaskIds = new Set<string>();
    const emittedGroups = new Set<string>();
    for (const task of tasks) {
        const isChapterDownload = task.taskTypeName === 'DownloadChapterTask' && 'chapter' in task;
        const groupTasks = isChapterDownload ? chapterTasksByManga.get(task.manga.mangaId) : undefined;

        if (!isChapterDownload || !groupTasks || groupTasks.length < 2) {
            rows.push(task);
            continue;
        }

        const mangaId = task.manga.mangaId;
        if (emittedGroups.has(mangaId)) continue;
        emittedGroups.add(mangaId);

        rows.push({ __group: true, mangaId, manga: task.manga, tasks: groupTasks });
        if (expandedGroups.value.has(mangaId)) {
            rows.push(...groupTasks);
            for (const groupTask of groupTasks) childTaskIds.add(groupTask.taskId);
        }
    }
    return { rows, childTaskIds };
});

const displayRows = computed((): DisplayRow[] => displayRowsResult.value.rows);
const childTaskIds = computed((): Set<string> => displayRowsResult.value.childTaskIds);

// Rows nested under an expanded group get an elevated background to set them apart visually.
const rowMeta = computed(() => ({
    class: {
        tr: (row: Row<DisplayRow>) => (!isGroupRow(row.original) && childTaskIds.value.has(row.original.taskId) ? 'bg-elevated' : ''),
    },
}));

const tableRef = useTemplateRef('tableRef');
defineExpose({ tableRef });

const sorting = ref<SortingState>([]);

const STATE_SORT_ORDER: Record<ServicesTasksTaskState, number> = { Failed: 0, Completed: 1, Running: 2, Queued: 3, Blocked: 4, Pending: 5 };

// A group row sorts by the most urgent state/soonest next run/latest last run among its tasks.
const rowState = (row: DisplayRow): ServicesTasksTaskState | undefined =>
    isGroupRow(row)
        ? row.tasks.reduce<ServicesTasksTaskState | undefined>(
              (worst, t) => (!worst || STATE_SORT_ORDER[t.status] < STATE_SORT_ORDER[worst] ? t.status : worst),
              undefined,
          )
        : row.status;

const rowChapterNumber = (row: DisplayRow): string | null => (!isGroupRow(row) && 'chapter' in row ? row.chapter.number : null);

const rowLastRun = (row: DisplayRow): string | null =>
    isGroupRow(row)
        ? row.tasks.reduce<string | null>((latest, t) => (t.lastRun && (!latest || t.lastRun > latest) ? t.lastRun : latest), null)
        : row.lastRun;

const rowNextRun = (row: DisplayRow): Date | undefined =>
    isGroupRow(row)
        ? row.tasks.reduce<Date | undefined>((soonest, t) => {
              const next = nextRun(t);
              return next && (!soonest || next < soonest) ? next : soonest;
          }, undefined)
        : nextRun(row);

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

// Nulls/undefined sort last regardless of the current sort direction.
const compareNullableDate = (a: Date | string | null | undefined, b: Date | string | null | undefined): number => {
    if (!a && !b) return 0;
    if (!a) return 1;
    if (!b) return -1;
    return new Date(a).getTime() - new Date(b).getTime();
};

const sortableHeader = (label: string) => {
    return ({ column }: { column: Column<DisplayRow, unknown> }) => {
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

const columns: TableColumn<DisplayRow>[] = [
    { accessorKey: 'expand', header: '', enableSorting: false },
    { accessorKey: 'type', header: 'Type', enableSorting: false },
    {
        accessorKey: 'state',
        header: sortableHeader('State'),
        sortingFn: (rowA, rowB) => {
            const a = rowState(rowA.original);
            const b = rowState(rowB.original);
            if (a === undefined && b === undefined) return 0;
            if (a === undefined) return 1;
            if (b === undefined) return -1;
            return STATE_SORT_ORDER[a] - STATE_SORT_ORDER[b];
        },
    },
    { accessorKey: 'manga', header: 'Manga', enableSorting: false },
    {
        accessorKey: 'chapter',
        header: sortableHeader('Chapter'),
        sortingFn: (rowA, rowB) => compareChapterNumber(rowChapterNumber(rowA.original), rowChapterNumber(rowB.original)),
    },
    {
        accessorKey: 'lastRun',
        header: sortableHeader('Last Run'),
        sortingFn: (rowA, rowB) => compareNullableDate(rowLastRun(rowA.original), rowLastRun(rowB.original)),
    },
    { accessorKey: 'interval', header: 'Interval', enableSorting: false },
    {
        accessorKey: 'nextRun',
        header: sortableHeader('Next Run'),
        sortingFn: (rowA, rowB) => compareNullableDate(rowNextRun(rowA.original), rowNextRun(rowB.original)),
    },
];
</script>
