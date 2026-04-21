<template>
    <UPageList>
        <TasksCard v-for="task in tasks" :key="task.taskId" :task="task" />
    </UPageList>
</template>

<script setup lang="ts">
import type { ServicesTasksChapterTask, ServicesTasksMangaTask, ServicesTasksTask } from '~/api/tranga';

defineProps<{ tasks?: (ServicesTasksTask | ServicesTasksMangaTask | ServicesTasksChapterTask)[] }>();
</script>
