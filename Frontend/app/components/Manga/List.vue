<template>
    <TrangaList :loading="loading" :empty="!mangas || mangas.length < 1">
        <UPageCard
            v-for="manga in mangas"
            :key="manga.mangaId"
            v-bind="selectMode ? {} : { to: `/manga/${manga.mangaId}` }"
            class="group relative isolate overflow-clip h-90 w-60 aspect-6/9 rounded-sm shadow-md ring-0 transition duration-200 hover:-translate-y-1 hover:shadow-xl"
            :class="selectMode && 'cursor-pointer'"
            :ui="{ container: 'p-0 sm:p-0' }"
            @click="selectMode ? $emit('select', manga) : useOverlay().closeAll()">
            <MangaCover
                :file-id="manga.metadataEntry?.coverId"
                :manga-id="manga.mangaId"
                :nsfw="!!manga.metadataEntry?.nsfw"
                :size="COVER_SIZES.card"
                class="absolute inset-0 z-0 size-full" />
            <div
                class="pointer-events-none absolute inset-x-0 bottom-0 z-1 flex flex-col gap-1 bg-linear-to-t from-black/90 via-black/55 to-transparent px-3 pb-3 pt-12">
                <UBadge v-if="manga.metadataEntry?.nsfw" label="NSFW" color="error" variant="solid" size="sm" class="self-start" />
                <p class="line-clamp-3 wrap-break-word text-[16.67px] font-semibold leading-tight text-white text-shadow-sm">
                    <TrangaTitle :title="manga.metadataEntry?.series" :max-length="1000" />
                </p>
            </div>
        </UPageCard>

        <template #empty>
            <!-- tour-intro: the first step of the guided tour points at this empty state (see plugins/tour.ts) -->
            <UEmpty
                icon="i-lucide-download"
                title="Nothing here!"
                description="Start searching..."
                class="tour-intro mx-auto h-min"
                variant="naked">
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
