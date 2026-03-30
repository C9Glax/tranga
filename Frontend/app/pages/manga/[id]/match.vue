<template>
    <MangaPage v-model="manga" :loading="statusManga">
        <MangaList v-model="match" :loading="matchStatus !== 'success'" />
    </MangaPage>
</template>

<script setup lang="ts">
import { useTranga } from '~/composables/trangaApi';
import type { GetMangaByMangaIdResponse, PostMangaByMangaIdMatchResponse } from '~/api/trangaApi';
import { MangaList } from '#components';

const mangaId = useRoute().params.id as string;

const { data: match, status: matchStatus } = await useTranga<PostMangaByMangaIdMatchResponse>(() => `/manga/${mangaId}/match`, {
    method: 'POST',
    lazy: true,
});

const { data: manga, status: statusManga } = await useTranga<GetMangaByMangaIdResponse>(() => `/manga/${mangaId}`, {
    key: ApiKeys.Manga(mangaId, []),
});
</script>
