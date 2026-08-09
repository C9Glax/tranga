<template>
    <UTable
        v-model:sorting="sorting"
        :data="chapters ?? []"
        :columns="columns"
        :loading="loading && !chapters"
        sticky
        class="w-full h-[70vh]">
        <template #volume-cell="{ row }">
            <UBadge v-if="row.original.volume" :label="row.original.volume" variant="outline" color="neutral" />
            <span v-else class="text-dimmed">-</span>
        </template>

        <template #title-cell="{ row }">
            <span v-if="row.original.title">{{ row.original.title }}</span>
            <span v-else class="text-dimmed">-</span>
        </template>

        <template #releaseDate-cell="{ row }">
            <TrangaTime v-if="row.original.releaseDate" :model-value="row.original.releaseDate" relative />
            <span v-else class="text-dimmed">-</span>
        </template>

        <template #status-cell="{ row }">
            <UBadge
                :label="row.original.isDownloaded ? 'Downloaded' : 'Not Downloaded'"
                :color="row.original.isDownloaded ? 'secondary' : 'neutral'"
                variant="subtle" />
        </template>
    </UTable>
</template>

<script setup lang="ts">
import type { ServicesMangaMangaChapter } from '~/api/tranga';
import type { TableColumn } from '@nuxt/ui/components/Table.vue';
import type { Column, SortingState } from '@tanstack/vue-table';

defineProps<{ chapters?: ServicesMangaMangaChapter[]; loading?: boolean }>();

const sorting = ref<SortingState>([
    { id: 'volume', desc: false },
    { id: 'number', desc: false },
]);

// Compares chapter/volume numbers numerically when possible, falls back to string compare, nulls sort last.
const compareChapterField = (a: string | null, b: string | null): number => {
    if (a === b) return 0;
    if (a === null) return 1;
    if (b === null) return -1;

    const numberA = parseFloat(a);
    const numberB = parseFloat(b);
    if (!isNaN(numberA) && !isNaN(numberB)) return numberA - numberB;

    return a.localeCompare(b);
};

const sortableHeader = (label: string) => {
    return ({ column }: { column: Column<ServicesMangaMangaChapter, unknown> }) => {
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

const columns: TableColumn<ServicesMangaMangaChapter>[] = [
    {
        accessorKey: 'volume',
        header: sortableHeader('Volume'),
        sortingFn: (rowA, rowB) => compareChapterField(rowA.original.volume, rowB.original.volume),
    },
    {
        accessorKey: 'number',
        header: sortableHeader('Number'),
        sortingFn: (rowA, rowB) => compareChapterField(rowA.original.number, rowB.original.number),
    },
    { accessorKey: 'title', header: 'Title', enableSorting: false },
    { accessorKey: 'releaseDate', header: 'Release Date', enableSorting: false },
    { accessorKey: 'status', header: 'Status', enableSorting: false },
];
</script>
