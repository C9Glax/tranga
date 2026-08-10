<template>
    <MangaPage :manga="manga" :actions="actions" :loading="statusManga !== 'success'">
        <DownloadLinkList
            :download-links="downloadLinks"
            :loading="statusDownloadLinks !== 'success'"
            empty-title="No Download Links"
            empty-description="This manga doesn't have any download links yet."
            :empty-actions="[moreDownloadLinksAction]" />
    </MangaPage>
</template>

<script setup lang="ts">
import type { GetMangasByMangaIdDownloadLinksResponse, GetMangasByMangaIdResponse, ServicesMangaManga } from '~/api/tranga';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';
import { ApiKeys } from '~/composables/ApiKeys';
import { syncMangaToKomga } from '~/utils/syncMangaToKomga';
import { patchMangaMonitored } from '~/utils/patchMangaMonitored';

const mangaId = useRoute().params.mangaId as string;

const { data: manga, status: statusManga } = await useTranga<GetMangasByMangaIdResponse>(() => `/mangas/${mangaId}`, {
    key: ApiKeys.Manga.Manga(mangaId),
});

const { data: downloadLinks, status: statusDownloadLinks } = useTranga<GetMangasByMangaIdDownloadLinksResponse>(
    () => `/mangas/${mangaId}/downloadLinks`,
    { key: ApiKeys.Manga.DownloadLinks(mangaId) },
);

const moreDownloadLinksAction = computed<ButtonProps>(() => ({
    label: 'More Download-Links',
    to: `/manga/${mangaId}/downloadLinks`,
    icon: 'i-lucide-download',
    variant: 'outline',
}));

const syncToKomgaAction = computed<ButtonProps>(() => ({
    label: 'Sync to Komga',
    icon: 'i-lucide-refresh-cw',
    variant: 'outline',
    onClick: () => syncMangaToKomga(mangaId),
}));

const removeAction = computed<ButtonProps>(() => ({
    label: manga.value?.monitored ? 'Remove' : 'Re-add',
    icon: manga.value?.monitored ? 'i-lucide-trash-2' : 'i-lucide-plus',
    color: manga.value?.monitored ? 'error' : 'primary',
    variant: 'outline',
    onClick: () => patchMangaMonitored(mangaId, !(manga.value?.monitored ?? true)),
}));

const actions = (_manga?: ServicesMangaManga): ButtonProps[] | undefined => [
    moreDownloadLinksAction.value,
    syncToKomgaAction.value,
    removeAction.value,
];
</script>
