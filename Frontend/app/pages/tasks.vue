<template>
    <TrangaPage :page-title="{ title: 'All Tasks', icon: { name: 'i-lucide-biceps-flexed' } }">
        <UPageSection :ui="{ container: 'sm:py-0 lg:py-0 gap-8 sm:gap-8' }">
            <TasksList :tasks="data" />
        </UPageSection>
    </TrangaPage>
</template>

<script setup lang="ts">
import type { GetTasksMangaByMangaIdResponse, GetTasksResponse } from '~/api/tranga';

const mangaId = useRoute().params.mangaId as string;

const { data, refresh } = await useTranga<GetTasksResponse>(() => `/tasks/manga/`, { key: ApiKeys.Tasks, lazy: true });

defineShortcuts({ meta_r: () => refresh() });
</script>
