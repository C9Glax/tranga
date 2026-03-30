<template>
    <UPageGrid :ui="{ base: 'grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-4 2xl:grid-cols-5' }">
        <USkeleton v-if="loading" v-for="_ in [...Array(3)]" class="w-60 h-90" />
        <MangaPreview v-else v-for="manga in model" :manga="manga" />
    </UPageGrid>
</template>

<script setup lang="ts">
import { MangaPreview } from '#components';

const model = defineModel<{ title: string; mangaId: string }[]>();

defineProps<{ loading?: boolean }>();
</script>
