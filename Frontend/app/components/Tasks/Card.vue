<template>
    <UPageCard>
        <template #body>
            {{ task.taskTypeName }}
        </template>
        <template #footer>
            <div>
                <UButton v-if="task.mangaId" :to="`/manga/${task.mangaId}`" label="Manga" />
                <UButton v-if="task.chapterId" :to="`/manga/${task.mangaId}`" label="Chapter" />
            </div>
        </template>
    </UPageCard>
</template>

<script setup lang="ts">
import type { ServicesTasksChapterTask, ServicesTasksMangaTask, ServicesTasksTask } from '~/api/tranga';

defineProps<{ task: ServicesTasksTask | ServicesTasksMangaTask | ServicesTasksChapterTask }>();
</script>
