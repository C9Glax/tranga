<template>
    <UPageGrid :ui="{ base: 'grid-cols-1 sm:grid-cols-2 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-3 2xl:grid-cols-4' }">
        <USkeleton v-if="loading" v-for="_ in [...Array(3)]" class="w-60 h-90" />

        <UPageCard
            v-if="!loading"
            v-for="manga in mangas"
            :to="`/manga/${manga.mangaId}`"
            :class="['relative overflow-clip', widthHeight]"
            :ui="{ container: 'p-0 sm:p-0' }"
            @click="useOverlay().closeAll()">
            <p class="z-10 absolute text-xl mx-2 my-3 font-bold text-shadow-sm">{{ manga.title }}</p>
            <MangaCover :mangaId="manga.mangaId" class="z-0 absolute" />
        </UPageCard>

        <div v-if="(mangas?.length ?? 0) < 1 && !loading" class="w-max flex gap-2">
            <UIcon name="i-lucide-brackets" class="size-15" />
            <p class="text-6xl inline">No Items</p>
        </div>
    </UPageGrid>
</template>

<script setup lang="ts">
import { MangaCover } from '#components';
import type { MangaDto } from '~/api/trangaApi';

const widthHeight = 'w-60 h-90';

defineProps<{ loading?: boolean; mangas?: MangaDto[] }>();
</script>
