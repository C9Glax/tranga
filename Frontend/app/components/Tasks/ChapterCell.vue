<template>
    <USkeleton v-if="status === 'pending'" class="h-6 w-32" />
    <div v-else-if="chapter" class="flex gap-1 items-center flex-wrap">
        <TrangaDoubleBadge v-if="chapter.volume" :first-badge-props="{ label: 'Vol' }" :second-badge-props="{ label: chapter.volume }" />
        <TrangaDoubleBadge :first-badge-props="{ label: 'Ch' }" :second-badge-props="{ label: chapter.number }" />
        <UBadge v-if="chapter.title" :label="chapter.title" variant="subtle" color="neutral" truncate />
    </div>
</template>

<script setup lang="ts">
import type { GetMangasChaptersByChapterIdResponse } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';

const props = defineProps<{ chapterId: string }>();

const { data: chapter, status } = await useTranga<GetMangasChaptersByChapterIdResponse>(() => `/mangas/chapters/${props.chapterId}`, {
    key: ApiKeys.Manga.Chapters.Chapter(props.chapterId),
});
</script>
