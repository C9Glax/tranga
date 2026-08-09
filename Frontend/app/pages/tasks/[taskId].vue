<template>
    <TrangaPage :page-title="{ title: 'Task', icon: { name: 'i-lucide-biceps-flexed' } }" rimless>
        <UPageSection :ui="{ container: 'px-0 max-w-none sm:py-0 lg:py-0 gap-4 sm:gap-4 mb-4' }">
            <div class="flex flex-wrap gap-3 items-center">
                <template v-if="task">
                    <UTooltip :text="`${task.taskType} · ${task.taskTypeId}`">
                        <UBadge
                            :icon="task.taskType === 'PeriodicTask' ? 'i-lucide-repeat' : 'i-lucide-line-dot-right-horizontal'"
                            :label="taskTypeLabel(task.taskTypeName)"
                            variant="subtle"
                            color="neutral" />
                    </UTooltip>
                    <UTooltip :text="taskStateDescription(task.status)">
                        <UBadge :label="task.status" :color="taskStateBadgeColor(task.status)" variant="subtle" />
                    </UTooltip>
                    <TasksMangaCell v-if="'manga' in task" :manga="task.manga" />
                    <TasksChapterCell v-if="'chapter' in task" :chapter="task.chapter" />
                    <TrangaTime v-if="task.lastRun" prefix="Last run" :model-value="task.lastRun" relative />
                </template>
                <USkeleton v-else class="h-lh w-64" />
            </div>
        </UPageSection>
        <UPageSection :ui="{ container: 'px-0 max-w-none sm:py-0 lg:py-0 gap-8 sm:gap-8' }">
            <TasksLogList :logs="logs" :loading="statusLogs !== 'success' && !logs?.length" />
        </UPageSection>
    </TrangaPage>
</template>

<script setup lang="ts">
import type { GetTasksByTaskIdResponse, GetTasksByTaskIdLogsResponse } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';

const taskId = useRoute().params.taskId as string;

const { data: task, refresh: refreshTask } = await useTranga<GetTasksByTaskIdResponse>(() => `/tasks/${taskId}`, {
    key: ApiKeys.Tasks.Task(taskId),
    lazy: true,
});

const {
    data: logs,
    status: statusLogs,
    refresh: refreshLogs,
} = await useTranga<GetTasksByTaskIdLogsResponse>(() => `/tasks/${taskId}/logs?limit=500`, { key: ApiKeys.Tasks.Logs(taskId), lazy: true });

const refresh = () => Promise.all([refreshTask(), refreshLogs()]);

defineShortcuts({ meta_r: () => refresh() });

let interval: number;
onMounted(() => {
    interval = setInterval(() => refresh(), 5000);
});
onUnmounted(() => clearInterval(interval));
</script>
