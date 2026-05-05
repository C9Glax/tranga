<template>
    <UPageList class="gap-4">
        <TasksCard v-for="task in tasks" :key="task.taskId" :task="task" />
    </UPageList>
</template>

<script setup lang="ts">
import type { ServicesTasksTask } from '~/api/tranga';

defineProps<{ tasks: ServicesTasksTask[] }>();
</script>
