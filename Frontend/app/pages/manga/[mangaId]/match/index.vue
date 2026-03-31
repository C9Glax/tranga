<template>
    <MangaPage :manga="manga" :actions="actions">
        <MangaMatchList :matches="matches" :loading="busy" />
    </MangaPage>
</template>

<script setup lang="ts">
import { MangaMatchList } from '#components';
import { useTranga } from '~/composables/trangaApi';
import type {
    GetMangaByMangaIdMatchedResponse,
    GetMangaByMangaIdResponse,
    MangaDto,
    PostMangaByMangaIdMatchResponse,
} from '~/api/trangaApi';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';

const mangaId = useRoute().params.mangaId as string;

const busy = computed(() => {
    return matchStatus.value !== 'success' || matchSearchStatus.value === 'pending';
});

const { data: matches, status: matchStatus } = await useTranga<GetMangaByMangaIdMatchedResponse>(() => `/manga/${mangaId}/matched`, {
    key: ApiKeys.MangaMatched(mangaId),
});

const { execute: search, status: matchSearchStatus } = await useTranga<PostMangaByMangaIdMatchResponse>(() => `/manga/${mangaId}/match`, {
    method: 'POST',
    immediate: false,
});

const { data: manga } = await useTranga<GetMangaByMangaIdResponse>(() => `/manga/${mangaId}`, { key: ApiKeys.Manga(mangaId) });

const actions = (manga?: MangaDto): ButtonProps[] => [
    {
        label: 'Find more',
        variant: 'soft',
        onClick: async () => {
            await search();
            await refreshNuxtData(ApiKeys.MangaMatched(mangaId));
        },
        icon: 'i-lucide-radar',
    },
];
</script>
