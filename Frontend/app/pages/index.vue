<template>
    <TrangaPage v-model:search="search" :page-title="{ title: 'Manga List', icon: { name: 'i-lucide-book' } }" showSearch>
        <UContainer>
            <MangaList :mangas="mangaList" :loading="status !== 'success'" />
        </UContainer>
    </TrangaPage>
</template>

<script setup lang="ts">
import { MangaList } from '#components';
import type { GetMangasResponse } from '~/api/trangaApi';

const search = ref<string>();

const { data, status, refresh } = await useTranga<GetMangasResponse>('/mangas', { key: ApiKeys.MangaList });

const mangaList = computed(() =>
    search.value
        ? data.value?.filter((m) => m.metadataEntry?.series.toLocaleLowerCase().includes(search.value!.toLocaleLowerCase()))
        : data.value
);

defineShortcuts({ shift_r: () => refresh() });
</script>
