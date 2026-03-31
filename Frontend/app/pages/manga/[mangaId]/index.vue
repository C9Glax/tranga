<template>
    <MangaPage :manga="manga" :actions="actions" :loading="statusManga !== 'success'">
        <UBlogPosts>
            <MetadataExtensionCard v-for="link in manga?.metadataLinks" :key="link.metadataLinkId" :metadata-link="link" />
            <DownloadExtensionCard v-for="link in manga?.downloadLinks" :key="link.downloadLinkId" :download-link="link" />
        </UBlogPosts>
    </MangaPage>
</template>

<script setup lang="ts">
import { MetadataExtensionCard, DownloadExtensionCard } from '#components';
import type { GetMangaByMangaIdResponse, MangaDto } from '~/api/trangaApi';
import { useTranga } from '~/composables/trangaApi';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';

const mangaId = useRoute().params.mangaId as string;

const { data: manga, status: statusManga } = await useTranga<GetMangaByMangaIdResponse>(() => `/manga/${mangaId}`, {
    key: ApiKeys.Manga(mangaId, ['DownloadLinks', 'MetadataLinks']),
    query: { includes: ['DownloadLinks', 'MetadataLinks'] },
});

const actions = (manga?: MangaDto): ButtonProps[] | undefined => [
    { label: 'Find Matches', to: `/manga/${manga?.mangaId}/match`, variant: 'soft' },
];
</script>
