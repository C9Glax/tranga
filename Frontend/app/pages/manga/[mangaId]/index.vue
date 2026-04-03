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
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';

const mangaId = useRoute().params.mangaId as string;

const { $tranga } = useNuxtApp();

const { data: manga, status: statusManga } = await useTranga<GetMangaByMangaIdResponse>(() => `/manga/${mangaId}`, {
    key: ApiKeys.Manga(mangaId, ['DownloadLinks', 'MetadataLinks']),
    query: { includes: ['DownloadLinks', 'MetadataLinks'] },
});

const busy = ref<boolean>(false);

const actions = (manga?: MangaDto): ButtonProps[] | undefined => [
    { label: 'Find Matches', to: `/manga/${manga?.mangaId}/match`, variant: 'soft' },
    manga?.monitored
        ? {
              label: 'Monitored',
              variant: 'soft',
              onClick: async () => await toggleMonitored(manga, false),
              icon: 'i-lucide-check',
              color: 'primary',
              disabled: busy.value,
          }
        : {
              label: 'Not Monitored',
              variant: 'outline',
              onClick: async () => await toggleMonitored(manga, true),
              icon: 'i-lucide-x',
              color: 'neutral',
              disabled: busy.value,
          },
];

async function toggleMonitored(manga: MangaDto | undefined, monitored: boolean) {
    if (!manga) return;
    try {
        busy.value = true;
        await $tranga(`/manga/${manga.mangaId}/monitor`, { method: 'patch', query: { monitored: monitored } });
        await refreshNuxtData([
            ApiKeys.MangaList(monitored),
            ApiKeys.Manga(manga?.mangaId),
            ApiKeys.Manga(manga?.mangaId, ['DownloadLinks']),
            ApiKeys.Manga(manga?.mangaId, ['MetadataLinks']),
            ApiKeys.Manga(manga?.mangaId, ['DownloadLinks', 'MetadataLinks']),
        ]);
    } finally {
        busy.value = false;
    }
}
</script>
