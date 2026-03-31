<template>
    <MangaPage :manga="manga" :cover-file-id="entry?.coverFileId ?? undefined" :title="entry?.title ?? undefined" :actions="actions">
        <template #description>
            <UEditor v-model="description" content-type="markdown" :editable="false" :ui="{ base: 'sm:px-0 p-0 px-0 ps-0' }" />
        </template>
        {{ chapters }}
    </MangaPage>
</template>

<script setup lang="ts">
import type {
    GetMangaByMangaIdResponse,
    GetMatchesByMatchIdChaptersResponse,
    GetMatchesByMatchIdResponse,
    MangaDto,
} from '~/api/trangaApi';
import { useTranga } from '~/composables/trangaApi';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';

const mangaId = useRoute().params.mangaId as string;
const matchId = useRoute().params.matchId as string;

const { data: manga } = await useTranga<GetMangaByMangaIdResponse>(() => `/manga/${mangaId}`, { key: ApiKeys.Manga(mangaId) });

const { data: entry } = await useTranga<GetMatchesByMatchIdResponse>(() => `/matches/${matchId}`, { key: ApiKeys.Match(matchId) });

const { data: chapters } = await useTranga<GetMatchesByMatchIdChaptersResponse>(() => `/matches/${matchId}/chapters`, {
    key: ApiKeys.Chapters(matchId),
});

const description = ref(entry.value?.description);

const actions = (manga?: MangaDto): ButtonProps[] | undefined => [
    { label: 'Linked', to: `/manga/${manga?.mangaId}`, icon: 'i-lucide-arrow-left', variant: 'outline' },
    { label: 'View', variant: 'soft', to: entry.value?.url ?? undefined, target: '_blank' },
    entry.value?.matched
        ? { label: 'Matched', variant: 'soft', onClick: toggleMatch, icon: 'i-lucide-check', color: 'primary', disabled: busy.value }
        : { label: 'Use as Match', variant: 'outline', onClick: toggleMatch, icon: 'i-lucide-x', color: 'neutral', disabled: busy.value },
];

const busy = ref<boolean>(false);

const toggleMatch = async () => {
    if (!entry.value) return;
    try {
        busy.value = true;
        await $tranga(`/matches/${matchId}`, { method: 'patch', query: { matched: !entry.value.matched } });
        await refreshNuxtData([
            ApiKeys.Match(matchId),
            ApiKeys.Manga(mangaId, ['DownloadLinks']),
            ApiKeys.Manga(mangaId, ['DownloadLinks', 'MetadataLinks']),
        ]);
    } finally {
        busy.value = false;
    }
};
</script>
