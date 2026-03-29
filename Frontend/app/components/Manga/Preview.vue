<template>
    <UPageCard
        :to="`/manga/${manga.mangaId}`"
        :class="['relative overflow-clip', widthHeight]"
        :ui="{ container: 'p-0 sm:p-0' }"
        @click="useOverlay().closeAll()">
        <p class="z-10 absolute text-xl mx-2 my-3 font-bold text-shadow-sm">{{ manga.title }}</p>
        <MangaCover :mangaId="manga.mangaId" class="z-0 absolute" />
    </UPageCard>
</template>

<script setup lang="ts">
import { MangaCover } from '#components';

const widthHeight = 'w-60 h-90';

const props = defineProps<{ manga: { title: string; mangaId: string } }>();
</script>
