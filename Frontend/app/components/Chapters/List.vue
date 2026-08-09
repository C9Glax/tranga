<template>
    <UTable :data="sorted" :columns="columns" :loading="loading && !chapters" sticky class="w-full h-[70vh]">
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

const props = defineProps<{ chapters?: ServicesMangaMangaChapter[]; loading?: boolean }>();

const sorted = computed((): ServicesMangaMangaChapter[] =>
    [...(props.chapters ?? [])].sort((a, b) => (parseFloat(a.number) || 0) - (parseFloat(b.number) || 0)),
);

const columns: TableColumn<ServicesMangaMangaChapter>[] = [
    { accessorKey: 'volume', header: 'Volume' },
    { accessorKey: 'number', header: 'Number' },
    { accessorKey: 'title', header: 'Title' },
    { accessorKey: 'releaseDate', header: 'Release Date' },
    { accessorKey: 'status', header: 'Status' },
];
</script>
