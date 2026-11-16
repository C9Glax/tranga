<template>
    <UTable ref="tableRef" :data="logs" :columns="columns" :loading="loading && !logs?.length" sticky class="w-full h-[70vh]">
        <template #timestamp-cell="{ row }">
            <TrangaTime :model-value="row.original.timestamp" relative />
        </template>

        <template #level-cell="{ row }">
            <UBadge :label="row.original.level" :color="taskLogLevelBadgeColor(row.original.level)" variant="subtle" />
        </template>

        <template #message-cell="{ row }">
            <span class="font-mono text-sm whitespace-pre-wrap">{{ row.original.message }}</span>
        </template>

        <template #loading><TrangaTableLoading /></template>
        <template #empty>
            <UEmpty
                icon="i-lucide-scroll-text"
                :title="$t('tasks.logs.empty.title')"
                :description="$t('tasks.logs.empty.description')"
                variant="naked" />
        </template>
    </UTable>
</template>

<script setup lang="ts">
import type { ServicesTasksTaskLogEntry } from '~/api/tranga';
import type { TableColumn } from '@nuxt/ui/components/Table.vue';

defineProps<{ logs?: ServicesTasksTaskLogEntry[]; loading?: boolean }>();

const { t } = useI18n();

const tableRef = useTemplateRef('tableRef');
defineExpose({ tableRef });

const columns = computed((): TableColumn<ServicesTasksTaskLogEntry>[] => [
    { accessorKey: 'timestamp', header: t('tasks.logs.columns.time') },
    { accessorKey: 'level', header: t('tasks.logs.columns.level') },
    { accessorKey: 'message', header: t('tasks.logs.columns.message') },
]);
</script>
