<template>
    <TrangaPage v-model:search="search" :page-title="{ title: 'Merge into...', icon: { name: 'i-lucide-git-merge' } }" search-enabled>
        <UContainer>
            <MangaList :mangas="mergeCandidates" :loading="status !== 'success'" select-mode @select="onSelectTarget" />
        </UContainer>
    </TrangaPage>
</template>

<script setup lang="ts">
import { MangaList, LazyMangaMergeConfirm } from '#components';
import type { GetMangasResponse, ServicesMangaManga } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';

const mangaId = useRoute().params.mangaId as string;
const search = ref<string>();

const { data, status } = await useTranga<GetMangasResponse>('/mangas', { key: ApiKeys.Manga.List });

const mergeCandidates = computed(() =>
    data.value
        ?.filter((m) => m.mangaId !== mangaId)
        .filter((m) => (search.value ? m.metadataEntry?.series.toLocaleLowerCase().includes(search.value!.toLocaleLowerCase()) : true)),
);

const mergeConfirmOverlay = useOverlay().create(LazyMangaMergeConfirm);

const onSelectTarget = (targetManga: ServicesMangaManga) => {
    mergeConfirmOverlay.open({ sourceMangaId: mangaId, targetManga });
};
</script>
