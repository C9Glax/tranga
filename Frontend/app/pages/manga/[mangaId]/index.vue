<template>
    <MangaPage :manga="manga" :actions="actions" :loading="statusManga !== 'success'"> </MangaPage>
</template>

<script setup lang="ts">
import type { GetMangasByMangaIdResponse, Manga } from '~/api/trangaApi';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';

const mangaId = useRoute().params.mangaId as string;

const { data: manga, status: statusManga } = await useTranga<GetMangasByMangaIdResponse>(() => `/mangas/${mangaId}`, {
    key: ApiKeys.Manga(mangaId),
});

const actions = (manga?: Manga): ButtonProps[] | undefined => [];
</script>
