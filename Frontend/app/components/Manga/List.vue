<template>
    <UPageGrid :ui="{ base: 'grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-4 2xl:grid-cols-5' }">
        <MangaPreview v-for="manga in items" :manga="manga" />
    </UPageGrid>
</template>

<script setup lang="ts">
import { MangaPreview } from '#components';

const props = defineProps<{ items?: { title: string; mangaId: string }[] }>();
</script>
