<template>
    <MangaList :items="manga" />
</template>

<script setup lang="ts">
import { MangaList } from '#components';
import { api } from '~/composables/api';

const includeUnmonitored = ref<boolean>(true);

const { data: manga } = await api.GET('/manga', {
    params: { query: { includeUnmonitored: includeUnmonitored.value }, key: ApiKeys.Manga(includeUnmonitored.value) },
});
</script>
