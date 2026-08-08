<template>
    <USkeleton v-if="status === 'pending'" class="h-6 w-32" />
    <UButton
        v-else-if="manga?.metadataEntry"
        :to="`/manga/${mangaId}`"
        variant="soft"
        color="primary"
        icon="i-lucide-book"
        truncate
        :label="manga.metadataEntry.series" />
    <UButton v-else :to="`/manga/${mangaId}`" variant="soft" color="neutral" icon="i-lucide-book" :label="mangaId" truncate />
</template>

<script setup lang="ts">
import type { GetMangasByMangaIdResponse } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';

const props = defineProps<{ mangaId: string }>();

const { data: manga, status } = await useTranga<GetMangasByMangaIdResponse>(() => `/mangas/${props.mangaId}`, {
    key: ApiKeys.Manga.Manga(props.mangaId),
});
</script>
