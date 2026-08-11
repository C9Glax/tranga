<template>
    <TrangaList :loading="loading" :empty="!mangas || mangas.length < 1">
        <UPageCard
            v-for="manga in mangas"
            :key="manga.mangaId"
            v-bind="selectMode ? {} : { to: `/manga/${manga.mangaId}` }"
            class="relative overflow-clip h-90 w-60 aspect-6/9"
            :class="selectMode && 'cursor-pointer'"
            :ui="{ container: 'p-0 sm:p-0' }"
            @click="selectMode ? $emit('select', manga) : useOverlay().closeAll()">
            <p class="z-1 absolute text-2xl mx-2 my-3 font-bold text-shadow-sm">
                <TrangaTitle :title="manga.metadataEntry?.series" />
                <UBadge v-if="manga?.metadataEntry?.nsfw" label="NSFW" color="error" variant="solid" />
            </p>
            <MangaCover
                :file-id="manga.metadataEntry?.coverId"
                :manga-id="manga.mangaId"
                no-blur
                class="z-0"
                :class="manga.metadataEntry?.nsfw ? 'blur-md' : 'blur-xs'" />
        </UPageCard>

        <template #empty>
            <UEmpty icon="i-lucide-download" title="Nothing here!" description="Start searching..." class="mx-auto h-min" variant="naked">
                <template #actions>
                    <UDashboardSearchButton color="secondary" />
                </template>
            </UEmpty>
        </template>
    </TrangaList>
</template>

<script setup lang="ts">
import { MangaCover } from '#components';
import type { ServicesMangaManga } from '~/api/tranga';

defineProps<{ loading?: boolean; mangas?: ServicesMangaManga[]; selectMode?: boolean }>();
defineEmits<{ select: [manga: ServicesMangaManga] }>();
</script>
