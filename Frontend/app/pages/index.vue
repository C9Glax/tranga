<template>
    <TrangaPage v-model:search="search" :page-title="{ title: 'Manga List', icon: { name: 'i-lucide-book' } }" searchEnabled>
        <UContainer>
            <MangaList :mangas="mangaList" :loading="status !== 'success'" />
        </UContainer>
    </TrangaPage>
</template>

<script setup lang="ts">
import { MangaList } from '#components';
import type { GetMangasResponse } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';

const search = ref<string>();

const { data, status, refresh } = await useTranga<GetMangasResponse>('/mangas', { key: ApiKeys.Manga.List });

const mangaList = computed(() =>
    search.value
        ? data.value?.filter((m) => m.metadataEntry?.series.toLocaleLowerCase().includes(search.value!.toLocaleLowerCase()))
        : data.value
);

defineShortcuts({ shift_r: () => refresh() });
</script>
