<template>
    <UTable
        v-model:sorting="sorting"
        :data="chapters ?? []"
        :columns="columns"
        :loading="loading && !chapters"
        sticky
        class="w-full h-full">
        <template #volume-cell="{ row }">
            <UBadge v-if="row.original.volume" :label="row.original.volume" variant="outline" color="neutral" />
            <span v-else class="text-dimmed">-</span>
        </template>

        <template #title-cell="{ row }">
            <ULink v-if="row.original.sourceUrl" :to="row.original.sourceUrl" target="_blank" class="hover:underline">
                <span v-if="row.original.title">{{ row.original.title }}</span>
                <span v-else class="text-dimmed">-</span>
            </ULink>
            <template v-else>
                <span v-if="row.original.title">{{ row.original.title }}</span>
                <span v-else class="text-dimmed">-</span>
            </template>
        </template>

        <template #releaseDate-cell="{ row }">
            <TrangaTime v-if="row.original.releaseDate" :model-value="row.original.releaseDate" relative />
            <span v-else class="text-dimmed">-</span>
        </template>

        <template #status-cell="{ row }">
            <UBadge
                :label="row.original.isDownloaded ? $t('chapters.downloaded') : $t('chapters.notDownloaded')"
                :color="row.original.isDownloaded ? 'secondary' : 'neutral'"
                variant="subtle" />
        </template>

        <template #actions-cell="{ row }">
            <UButton
                icon="i-lucide-trash-2"
                color="error"
                variant="ghost"
                :aria-label="$t('chapters.delete')"
                @click="onDelete(row.original)" />
        </template>

        <template #loading><TrangaTableLoading /></template>
        <template #empty>
            <UEmpty
                icon="i-lucide-book-open"
                :title="$t('chapters.empty.title')"
                :description="$t('chapters.empty.description')"
                variant="naked" />
        </template>
    </UTable>
</template>

<script setup lang="ts">
import { TrangaConfirmModal } from '#components';
import type { ServicesMangaMangaChapter } from '~/api/tranga';
import type { TableColumn } from '@nuxt/ui/components/Table.vue';
import type { Column, SortingState } from '@tanstack/vue-table';

defineProps<{ chapters?: ServicesMangaMangaChapter[]; loading?: boolean }>();

const { t } = useI18n();

const confirmModal = useOverlay().create(TrangaConfirmModal);

const chapterLabel = (chapter: ServicesMangaMangaChapter) =>
    [
        chapter.volume ? t('chapters.volumeShort', { volume: chapter.volume }) : null,
        t('chapters.chapterShort', { number: chapter.number }),
        chapter.title,
    ]
        .filter(Boolean)
        .join(' - ');

const onDelete = (chapter: ServicesMangaMangaChapter) => {
    confirmModal.open({
        title: t('chapters.delete'),
        description: t('chapters.deleteDescription', { label: chapterLabel(chapter) }),
        onConfirm: () => deleteChapter(chapter.mangaId, chapter.chapterId),
    });
};

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

const columns = computed((): TableColumn<ServicesMangaMangaChapter>[] => [
    {
        accessorKey: 'volume',
        header: sortableHeader(t('chapters.columns.volume')),
        sortingFn: (rowA, rowB) => compareChapterField(rowA.original.volume, rowB.original.volume),
    },
    {
        accessorKey: 'number',
        header: sortableHeader(t('chapters.columns.number')),
        sortingFn: (rowA, rowB) => compareChapterField(rowA.original.number, rowB.original.number),
    },
    { accessorKey: 'title', header: t('chapters.columns.title'), enableSorting: false },
    { accessorKey: 'releaseDate', header: t('chapters.columns.releaseDate'), enableSorting: false },
    { accessorKey: 'status', header: t('chapters.columns.status'), enableSorting: false },
    { id: 'actions', header: '', enableSorting: false },
]);
</script>
