<template>
    <UPageGrid :ui="{ base: 'grid-cols-1 sm:grid-cols-2 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-3 2xl:grid-cols-4' }">
        <USkeleton v-if="loading" v-for="_ in [...Array(3)]" class="w-60 h-90" />

        <UPageCard
            v-if="!loading"
            v-for="manga in mangas"
            :to="`/manga/${manga.mangaId}`"
            class="relative overflow-clip"
            :ui="{ container: 'p-0 sm:p-0' }"
            @click="useOverlay().closeAll()">
            <p class="z-1 absolute text-2xl mx-2 my-3 font-bold text-shadow-sm">
                {{ manga.metadataEntry?.series }}
                <UBadge v-if="manga?.metadataEntry?.nsfw" label="NSFW" color="error" variant="solid" />
            </p>
            <MangaCover
                :file-id="manga.metadataEntry?.coverId"
                :manga-id="manga.mangaId"
                noBlur
                class="z-0"
                :class="manga.metadataEntry?.nsfw ? 'blur-md' : 'blur-xs'" />
        </UPageCard>

        <div v-if="(mangas?.length ?? 0) < 1 && !loading" class="w-max flex gap-2">
            <UIcon name="i-lucide-brackets" class="size-15" />
            <p class="text-6xl inline">No Items</p>
        </div>
    </UPageGrid>
</template>

<script setup lang="ts">
import { MangaCover } from '#components';
import type { ServicesMangaManga } from '~/api/tranga';

defineProps<{ loading?: boolean; mangas?: ServicesMangaManga[] }>();
</script>
