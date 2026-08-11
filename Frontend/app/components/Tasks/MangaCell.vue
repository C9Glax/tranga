<template>
    <UButton v-if="manga.series" :to="`/manga/${manga.mangaId}`" variant="soft" color="primary" icon="i-lucide-book">
        <TrangaTitle :title="manga.series" />
    </UButton>
    <UButton v-else :to="`/manga/${manga.mangaId}`" variant="soft" color="neutral" icon="i-lucide-book" :label="manga.mangaId" truncate />
</template>

<script setup lang="ts">
import type { ServicesTasksMangaSummary } from '~/api/tranga';

defineProps<{ manga: ServicesTasksMangaSummary }>();
</script>
