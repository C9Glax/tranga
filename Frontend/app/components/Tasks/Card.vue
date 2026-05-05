<template>
    <UPageCard>
        <template #header>
            {{ task.taskTypeName }}
        </template>
        <template #footer>
            <div>
                <UButton v-if="task.mangaId" :to="`/manga/${task.mangaId}`" label="Manga" />
                <UButton v-if="task.mangaId" :to="`/manga/${task.mangaId}`" label="Chapter" />
            </div>
            {{ task }}
        </template>
    </UPageCard>
</template>

<script setup lang="ts">
import type { ServicesTasksTask } from '~/api/tranga';

defineProps<{ task: ServicesTasksTask }>();
</script>
