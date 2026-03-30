<template>
    <MangaPage v-model="manga" :actions="(m) => [{ label: 'Match', to: `/manga/${m?.mangaId}/match` }]">
        <UBlogPosts>
            <MetadataExtensionOverview v-for="link in manga?.metadataLinks" :key="link.metadataLinkId" :metadata-link="link" />
        </UBlogPosts>
    </MangaPage>
</template>

<script setup lang="ts">
import { MetadataExtensionOverview } from '#components';
import type { GetMangaByMangaIdResponse } from '~/api/trangaApi';
import { useTranga } from '~/composables/trangaApi';

const mangaId = useRoute().params.id as string;

const { data: manga } = await useTranga<GetMangaByMangaIdResponse>(() => `/manga/${mangaId}`, {
    key: ApiKeys.Manga(mangaId, ['DownloadLinks', 'MetadataLinks']),
    query: { includes: ['DownloadLinks', 'MetadataLinks'] },
});
</script>
