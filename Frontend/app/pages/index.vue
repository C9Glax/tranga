<template>
    <UPage>
        <USlideover side="left" title="Options">
            <UButton label="View" color="neutral" variant="soft" class="fixed left-0 top-(--ui-header-height) m-4" />

            <template #body>
                <USwitch v-model="includeUnmonitored" label="Include Unmonitored Manga" />
            </template>
        </USlideover>
        <MangaList :mangas="mangaList" :loading="status !== 'success'" />
    </UPage>
</template>

<script setup lang="ts">
import { MangaList } from '#components';
import type { GetMangasResponse } from '~/api/trangaApi';

const includeUnmonitored = useState<boolean>(() => false);

const {
    data: mangaList,
    status,
    refresh,
} = await useTranga<GetMangasResponse>('/mangas', {
    query: { includeUnmonitored: includeUnmonitored },
    key: ApiKeys.MangaList(includeUnmonitored.value),
});

defineShortcuts({ shift_r: () => refresh() });
</script>
