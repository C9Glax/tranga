<template>
    <UPage>
        <MangaList :mangas="mangaList" :loading="status !== 'success'" />
    </UPage>
</template>

<script setup lang="ts">
import { MangaList } from '#components';
import type { GetMangasResponse } from '~/api/trangaApi';

const { data: mangaList, status, refresh } = await useTranga<GetMangasResponse>('/mangas', { key: ApiKeys.MangaList });

defineShortcuts({ shift_r: () => refresh() });
</script>
