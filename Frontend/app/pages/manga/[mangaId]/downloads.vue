<template>
    <TrangaPage
        :navigation-props="{ items: [{ label: 'To Manga', icon: 'i-lucide-book', to: `/manga/${mangaId}` }] }"
        :page-title="{ title: 'Manga active Downloads', icon: { name: 'i-lucide-cloud-download' } }">
        <UPageSection :ui="{ container: 'sm:py-0 lg:py-0 gap-8 sm:gap-8' }">
            <TasksList :tasks="data" />
        </UPageSection>
    </TrangaPage>
</template>

<script setup lang="ts">
import type { GetTasksMangaByMangaIdDownloadsResponse } from '~/api/tranga';

const mangaId = useRoute().params.mangaId as string;

const { data, refresh } = await useTranga<GetTasksMangaByMangaIdDownloadsResponse>(() => `/tasks/manga/${mangaId}/downloads`, {
    key: ApiKeys.MangaDownloadTasks(mangaId),
    lazy: true,
});

defineShortcuts({ meta_r: () => refresh() });
</script>
