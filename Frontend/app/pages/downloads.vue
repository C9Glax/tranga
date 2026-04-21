<template>
    <TrangaPage :page-title="{ title: 'Active Downloads', icon: { name: 'i-lucide-cloud-download' } }">
        <UPageSection :ui="{ container: 'sm:py-0 lg:py-0 gap-8 sm:gap-8' }">
            <TasksList :tasks="data" />
        </UPageSection>
    </TrangaPage>
</template>

<script setup lang="ts">
import type { GetTasksMangaDownloadsResponse } from '~/api/tranga';

const mangaId = useRoute().params.mangaId as string;

const { data, refresh } = await useTranga<GetTasksMangaDownloadsResponse>(() => `/tasks/manga/downloads`, {
    key: ApiKeys.DownloadTasks,
    lazy: true,
});

defineShortcuts({ meta_r: () => refresh() });
</script>
