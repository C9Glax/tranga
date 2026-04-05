<template>
    <MangaPage :manga="manga" :actions="actions" :loading="statusManga !== 'success'"> </MangaPage>
</template>

<script setup lang="ts">
import type { GetMangasByMangaIdResponse, MangaMetadata } from '~/api/trangaApi';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';

const mangaId = useRoute().params.mangaId as string;

const { data: manga, status: statusManga } = await useTranga<GetMangasByMangaIdResponse>(() => `/mangas/${mangaId}`, {
    key: ApiKeys.Manga(mangaId),
});

const actions = (manga?: MangaMetadata): ButtonProps[] | undefined => [
    { label: 'Find Matches', to: `/mangas/${manga?.mangaId}/match`, variant: 'soft' },
];
</script>
