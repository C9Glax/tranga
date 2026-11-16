<template>
    <MangaPage :manga="manga" :actions="actions" :loading="statusManga !== 'success'">
        <DownloadLinkList
            :download-links="downloadLinks"
            :loading="statusDownloadLinks !== 'success'"
            :empty-title="$t('manga.noDownloadLinks.title')"
            :empty-description="$t('manga.noDownloadLinks.description')"
            :empty-actions="[moreDownloadLinksAction]" />
    </MangaPage>
</template>

<script setup lang="ts">
import type { GetMangasByMangaIdDownloadLinksResponse, GetMangasByMangaIdResponse, ServicesMangaManga } from '~/api/tranga';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';
import { ApiKeys } from '~/composables/ApiKeys';
import { syncMangaToKomga } from '~/utils/syncMangaToKomga';
import { removeManga } from '~/utils/removeManga';

const mangaId = useRoute().params.mangaId as string;
const { t } = useI18n();

const { data: manga, status: statusManga } = await useTranga<GetMangasByMangaIdResponse>(() => `/mangas/${mangaId}`, {
    key: ApiKeys.Manga.Manga(mangaId),
});

const { data: downloadLinks, status: statusDownloadLinks } = useTranga<GetMangasByMangaIdDownloadLinksResponse>(
    () => `/mangas/${mangaId}/downloadLinks`,
    { key: ApiKeys.Manga.DownloadLinks(mangaId) },
);

const moreDownloadLinksAction = computed<ButtonProps>(() => ({
    label: t('manga.moreDownloadLinks'),
    to: `/manga/${mangaId}/downloadLinks`,
    icon: 'i-lucide-download',
    variant: 'outline',
    class: 'tour-more-download-links',
}));

const syncToKomgaAction = computed<ButtonProps>(() => ({
    label: t('manga.syncToLibrary'),
    icon: 'i-lucide-refresh-cw',
    variant: 'outline',
    onClick: () => syncMangaToKomga(mangaId),
}));

const mergeAction = computed<ButtonProps>(() => ({
    label: t('manga.mergeInto'),
    icon: 'i-lucide-git-merge',
    variant: 'outline',
    to: `/manga/${mangaId}/merge`,
}));

const removeAction = computed<ButtonProps>(() => ({
    label: t('manga.remove'),
    icon: 'i-lucide-trash-2',
    color: 'error',
    variant: 'outline',
    onClick: () => removeManga(mangaId),
}));

const actions = (_manga?: ServicesMangaManga): ButtonProps[] | undefined => [
    moreDownloadLinksAction.value,
    syncToKomgaAction.value,
    mergeAction.value,
    removeAction.value,
];
</script>
