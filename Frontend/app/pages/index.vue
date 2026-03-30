<template>
    <UPage>
        <USlideover side="left" title="Options">
            <UButton label="View" color="neutral" variant="soft" class="fixed left-0 top-(--ui-header-height) m-4" />

            <template #body>
                <USwitch v-model="includeUnmonitored" label="Include Unmonitored Manga" />
            </template>
        </USlideover>
        <MangaList v-model="mangaList" :loading="status !== 'success'" />
    </UPage>
</template>

<script setup lang="ts">
import { MangaList } from '#components';
import { useTranga } from '~/composables/trangaApi';
import type { GetMangaResponse } from '~/api/trangaApi';

const includeUnmonitored = ref<boolean>(true);

const {
    data: mangaList,
    status,
    refresh,
} = await useTranga<GetMangaResponse>('/manga', {
    query: { includeUnmonitored: includeUnmonitored.value },
    key: ApiKeys.MangaList(includeUnmonitored.value),
});

defineShortcuts({ shift_r: () => refresh() });
</script>
