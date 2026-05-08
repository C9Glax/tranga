<template>
    <UCard>
        <div class="flex flex-col gap-4 *:shrink">
            <UTooltip :text="`${task.taskType} ${task.taskTypeId}`">
                <UUser
                    :name="splitCamelCase(task.taskTypeName)"
                    :description="task.taskId"
                    :avatar="{ icon: task.taskType === 'PeriodicTask' ? 'i-lucide-repeat' : 'i-lucide-line-dot-right-horizontal' }" />
            </UTooltip>
            <MetadataListCard v-if="manga?.metadataEntry" :metadata="manga.metadataEntry" :manga-id="manga.mangaId" class="max-h-1/4">
                <template #actions>
                    <div v-if="chapter" class="flex gap-4">
                        <TrangaDoubleBadge
                            v-if="chapter.title"
                            :first-badge-props="{ label: 'Title' }"
                            :second-badge-props="{ label: chapter.title }" />
                        <TrangaDoubleBadge
                            v-if="chapter.volume"
                            :first-badge-props="{ label: 'Volume' }"
                            :second-badge-props="{ label: chapter.volume }" />
                        <TrangaDoubleBadge :first-badge-props="{ label: 'Chapter' }" :second-badge-props="{ label: chapter.number }" />
                        <TrangaTime prefix="Release Date" :modelValue="chapter.releaseDate" />
                        <TrangaTime :modelValue="chapter.releaseDate" relative />
                    </div>
                </template>
            </MetadataListCard>
        </div>

        <template #footer>
            <div class="flex justify-between">
                <div v-if="task.lastRun" class="flex gap-2 items-center">
                    <TrangaTime v-model="task.lastRun" prefix="Last Run" />
                    <UIcon v-if="task.interval" name="i-lucide-plus" />
                    <TrangaDoubleBadge
                        v-if="task.interval"
                        :first-badge-props="{ label: 'Interval' }"
                        :second-badge-props="{ label: task.interval }" />
                    <UIcon v-if="nextRun" name="i-lucide-arrow-right" />
                    <TrangaTime v-if="nextRun" v-model="nextRun" prefix="Next Run" relative />
                </div>
                <UBadge v-else color="secondary" label="Running" />
                <UFieldGroup>
                    <UButton v-if="task.mangaId" :to="`/manga/${task.mangaId}`" label="Manga" variant="soft" />
                    <UButton v-if="task.chapterId" :to="`/manga/${task.mangaId}`" label="Chapter" variant="soft" color="secondary" />
                </UFieldGroup>
            </div>
        </template>
    </UCard>
</template>

<script setup lang="ts">
import type { ServicesTasksTask, GetMangasChaptersByChapterIdResponse, GetMangasByMangaIdResponse } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';

const props = defineProps<{ task: ServicesTasksTask }>();

const nextRun = computed((): Date | undefined => {
    if (!props.task.lastRun || !props.task.interval) return undefined;
    const nextRun = new Date(props.task.lastRun);
    const interval = parseTimespan(props.task.interval);
    if (!interval) return undefined;
    nextRun.setTime(nextRun.getTime() + interval);
    return nextRun;
});

const { data: manga } = await useTranga<GetMangasByMangaIdResponse>(() => `/mangas/${props.task.mangaId}`, {
    key: ApiKeys.Manga.Manga(props.task.mangaId ?? ''),
    immediate: props.task.mangaId !== null && props.task.mangaId !== undefined,
});

const { data: chapter } = await useTranga<GetMangasChaptersByChapterIdResponse>(() => `/mangas/chapters/${props.task.chapterId}`, {
    key: ApiKeys.Manga.Chapters.Chapter(props.task.chapterId ?? ''),
    immediate: props.task.chapterId !== null && props.task.chapterId !== undefined,
});
</script>
