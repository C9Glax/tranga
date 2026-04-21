<template>
    <TrangaPage
        :navigation-props="{ items: [{ label: 'To Manga', icon: 'i-lucide-book', to: `/manga/${mangaId}` }] }"
        :page-title="{ title: 'Manga Tasks', icon: { name: 'i-lucide-biceps-flexed' } }">
        <UPageSection :ui="{ container: 'sm:py-0 lg:py-0 gap-8 sm:gap-8' }">
            <TasksList :tasks="data" />
        </UPageSection>
    </TrangaPage>
</template>

<script setup lang="ts">
import type { GetTasksMangaByMangaIdResponse } from '~/api/tranga';

const mangaId = useRoute().params.mangaId as string;

const { data, refresh } = await useTranga<GetTasksMangaByMangaIdResponse>(() => `/tasks/manga/${mangaId}`, {
    key: ApiKeys.MangaTasks(mangaId),
    lazy: true,
});

defineShortcuts({ meta_r: () => refresh() });
</script>
