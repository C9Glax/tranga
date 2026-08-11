<template>
    <div class="flex flex-row flex-wrap gap-2">
        <USkeleton v-for="_ in [...Array(3)]" v-if="loading" class="w-60 h-90" />

        <UPageCard
            v-for="manga in mangas"
            v-if="!loading"
            v-bind="selectMode ? {} : { to: `/manga/${manga.mangaId}` }"
            class="relative overflow-clip"
            :class="selectMode && 'cursor-pointer'"
            :ui="{ container: 'p-0 sm:p-0' }"
            @click="selectMode ? $emit('select', manga) : useOverlay().closeAll()">
            <p class="z-1 absolute text-2xl mx-2 my-3 font-bold text-shadow-sm">
                {{ manga.metadataEntry?.series }}
                <UBadge v-if="manga?.metadataEntry?.nsfw" label="NSFW" color="error" variant="solid" />
            </p>
            <MangaCover
                :file-id="manga.metadataEntry?.coverId"
                :manga-id="manga.mangaId"
                no-blur
                class="z-0"
                :class="manga.metadataEntry?.nsfw ? 'blur-md' : 'blur-xs'" />
        </UPageCard>

        <div v-if="(mangas?.length ?? 0) < 1 && !loading" class="w-max flex gap-2">
            <UIcon name="i-lucide-brackets" class="size-15" />
            <p class="text-6xl inline">No Items</p>
        </div>
    </div>
</template>

<script setup lang="ts">
import { MangaCover } from '#components';
import type { ServicesMangaManga } from '~/api/tranga';

defineProps<{ loading?: boolean; mangas?: ServicesMangaManga[]; selectMode?: boolean }>();
defineEmits<{ select: [manga: ServicesMangaManga] }>();
</script>
