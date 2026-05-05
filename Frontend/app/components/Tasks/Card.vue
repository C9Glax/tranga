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
        </template>
        <TrangaDoubleBadge v-if="task.interval" :first-badge-props="{ label: 'Interval' }" :second-badge-props="{ label: task.interval }" />
        <TrangaTime v-if="task.lastRun" v-model="task.lastRun" prefix="Last Run" variant="outline" />
    </UPageCard>
</template>

<script setup lang="ts">
import type { ServicesTasksTask } from '~/api/tranga';

defineProps<{ task: ServicesTasksTask }>();
</script>
