<template>
    <MetadataPage :metadata="metadata" :actions="actions" :loading="statusMetadata !== 'success'" />
</template>

<script setup lang="ts">
import type {
    GetMangasMetadataByMetadataIdMangaRelatedResponse,
    GetMangasMetadataByMetadataIdMangaResponse,
    GetMangasMetadataByMetadataIdResponse,
} from '~/api/tranga';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';
import { patchMangaMetadataSource } from '~/utils/patchMangaMetadataSource';
import { ApiKeys } from '~/composables/ApiKeys';
import Manga = ApiKeys.Manga;

const metadataId = useRoute().params.metadataId as string;
const mangaId = useRoute().query.mangaId as string | undefined;

const { t } = useI18n();

const { data: metadata, status: statusMetadata } = await useTranga<GetMangasMetadataByMetadataIdResponse>(
    () => `/mangas/metadata/${metadataId}`,
    { key: Manga.Metadata.Entry(metadataId) },
);

const { data: manga } = await useTranga<GetMangasMetadataByMetadataIdMangaResponse>(() => `/mangas/metadata/${metadataId}/manga`, {
    key: Manga.Metadata.Manga(metadataId),
});

const { data: relatedMangaIds } = await useTranga<GetMangasMetadataByMetadataIdMangaRelatedResponse>(
    () => `/mangas/metadata/${metadataId}/manga/related`,
    { key: Manga.Metadata.RelatedManga(metadataId) },
);

const actions = (): ButtonProps[] | undefined => {
    const items: ButtonProps[] = [];

    if (mangaId && relatedMangaIds.value?.find((m) => m === mangaId) && !manga.value) {
        items.push({
            label: t('metadata.goToManga'),
            icon: 'i-lucide-book',
            to: `/manga/${mangaId}`,
            target: '_blank',
            variant: 'soft',
            class: 'tour-go-to-manga',
        });
        items.push({
            label: t('metadata.useAsSource'),
            onClick: async () => await patchMangaMetadataSource(metadataId, mangaId),
            variant: 'solid',
            class: 'tour-use-as-source',
        });
    } else if (manga.value) {
        items.push({
            label: t('metadata.goToManga'),
            icon: 'i-lucide-book',
            to: `/manga/${manga.value.mangaId}`,
            variant: 'soft',
            class: 'tour-go-to-manga',
        });
        items.push({ label: t('metadata.isSource'), disabled: true, variant: 'outline' });
    } else if (relatedMangaIds.value?.length === 1) {
        items.push({
            label: t('metadata.useAsSource'),
            onClick: async () => await patchMangaMetadataSource(metadataId, relatedMangaIds.value![0]!),
            variant: 'solid',
            class: 'tour-use-as-source',
        });
    }

    return items;
};
</script>
