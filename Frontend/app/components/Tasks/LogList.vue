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
    </UTable>
</template>

<script setup lang="ts">
import type { ServicesTasksTaskLogEntry } from '~/api/tranga';
import type { TableColumn } from '@nuxt/ui/components/Table.vue';

defineProps<{ logs?: ServicesTasksTaskLogEntry[]; loading?: boolean }>();

const tableRef = useTemplateRef('tableRef');
defineExpose({ tableRef });

const columns: TableColumn<ServicesTasksTaskLogEntry>[] = [
    { accessorKey: 'timestamp', header: 'Time' },
    { accessorKey: 'level', header: 'Level' },
    { accessorKey: 'message', header: 'Message' },
];
</script>
