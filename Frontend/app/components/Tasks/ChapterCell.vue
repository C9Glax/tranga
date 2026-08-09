<template>
    <div class="flex gap-1 items-center flex-wrap">
        <TrangaDoubleBadge v-if="chapter.volume" :first-badge-props="{ label: 'Vol' }" :second-badge-props="{ label: chapter.volume }" />
        <TrangaDoubleBadge :first-badge-props="{ label: 'Ch' }" :second-badge-props="{ label: chapter.number }" />
        <UBadge v-if="chapter.title" :label="chapter.title" variant="subtle" color="neutral" truncate />
    </div>
</template>

<script setup lang="ts">
import type { ServicesTasksChapterSummary } from '~/api/tranga';

defineProps<{ chapter: ServicesTasksChapterSummary }>();
</script>
