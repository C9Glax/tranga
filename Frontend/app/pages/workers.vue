<template>
    <TrangaPage rimless>
        <UPageSection :ui="{ container: 'py-0 sm:py-0 lg:py-0 px-0 sm:px-0 lg:px-0' }">
            <WorkersList :workers="workers" :tasks="tasks" :loading="workersStatus !== 'success'" />
        </UPageSection>
    </TrangaPage>
</template>

<script setup lang="ts">
import type { GetTasksWorkersResponse, GetTasksResponse } from '~/api/tranga';

const {
    data: workers,
    refresh: refreshWorkers,
    status: workersStatus,
} = await useTranga<GetTasksWorkersResponse>('/tasks/workers', { lazy: true });

const { data: tasks, refresh: refreshTasks } = await useTranga<GetTasksResponse>(() => `/tasks?includeFinished=true`, { lazy: true });

defineShortcuts({ meta_r: () => refresh() });

const refresh = () => Promise.all([refreshWorkers(), refreshTasks()]);

let interval: number;
onMounted(() => {
    interval = setInterval(() => refresh(), 5000);
});
onUnmounted(() => clearInterval(interval));
</script>
