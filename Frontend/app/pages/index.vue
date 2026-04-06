<template>
    <TrangaPage :page-title="{ title: 'Manga List', icon: { name: 'i-lucide-book' } }">
        <UContainer>
            <MangaList :mangas="mangaList" :loading="status !== 'success'" />
        </UContainer>
    </TrangaPage>
</template>

<script setup lang="ts">
import { MangaList } from '#components';
import type { GetMangasResponse } from '~/api/trangaApi';

const { data: mangaList, status, refresh } = await useTranga<GetMangasResponse>('/mangas', { key: ApiKeys.MangaList });

defineShortcuts({ shift_r: () => refresh() });
</script>
