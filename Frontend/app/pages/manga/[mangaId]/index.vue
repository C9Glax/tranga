<template>
    <MangaPage :manga="manga" :actions="actions" :loading="statusManga !== 'success'">
        <DownloadLinkList :download-links="downloadLinks" :loading="statusDownloadLinks !== 'success'" />
    </MangaPage>
</template>

<script setup lang="ts">
import type { GetMangasByMangaIdDownloadLinksResponse, GetMangasByMangaIdResponse, Manga } from '~/api/trangaApi';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';

const mangaId = useRoute().params.mangaId as string;

const { data: manga, status: statusManga } = await useTranga<GetMangasByMangaIdResponse>(() => `/mangas/${mangaId}`, {
    key: ApiKeys.Manga(mangaId),
});

const { data: downloadLinks, status: statusDownloadLinks } = useTranga<GetMangasByMangaIdDownloadLinksResponse>(
    () => `/mangas/${mangaId}/downloadLinks`,
    { key: ApiKeys.MangaDownloadLinks(mangaId) }
);

const actions = (manga?: Manga): ButtonProps[] | undefined => [
    { label: 'More Download-Links', to: `/manga/${manga?.mangaId}/downloadLinks`, icon: 'i-lucide-download', variant: 'outline' },
];
</script>
