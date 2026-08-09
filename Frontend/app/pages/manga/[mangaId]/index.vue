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
import type {
    GetLibrariesMappingsByMangaIdResponse,
    GetMangasByMangaIdDownloadLinksResponse,
    GetMangasByMangaIdResponse,
    ServicesMangaManga,
} from '~/api/tranga';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';
import { ApiKeys } from '~/composables/ApiKeys';

const mangaId = useRoute().params.mangaId as string;

const { data: manga, status: statusManga } = await useTranga<GetMangasByMangaIdResponse>(() => `/mangas/${mangaId}`, {
    key: ApiKeys.Manga.Manga(mangaId),
});

const { data: downloadLinks, status: statusDownloadLinks } = useTranga<GetMangasByMangaIdDownloadLinksResponse>(
    () => `/mangas/${mangaId}/downloadLinks`,
    { key: ApiKeys.Manga.DownloadLinks(mangaId) },
);

const { data: libraryMappings } = useTranga<GetLibrariesMappingsByMangaIdResponse>(() => `/libraries/mappings/${mangaId}`, {
    key: ApiKeys.Libraries.Mapping(mangaId),
});

const moreDownloadLinksAction = computed<ButtonProps>(() => ({
    label: 'More Download-Links',
    to: `/manga/${mangaId}/downloadLinks`,
    icon: 'i-lucide-download',
    variant: 'outline',
}));

const actions = (_manga?: ServicesMangaManga): ButtonProps[] | undefined => [
    moreDownloadLinksAction.value,
    ...(libraryMappings.value ?? []).map(
        (mapping): ButtonProps => ({
            label: 'View in Komga',
            to: mapping.seriesUrl,
            icon: 'i-lucide-external-link',
            variant: 'outline',
            target: '_blank',
        }),
    ),
];
</script>
