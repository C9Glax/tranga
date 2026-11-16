<template>
    <TrangaPage :navigation="navigation">
        <UPageSection :ui="{ container: 'sm:py-0 lg:py-0 gap-8 sm:gap-8' }">
            <MetadataList :metadata-list="metadataSources" :loading="statusMetadata !== 'success'" :manga-id="mangaId" />
        </UPageSection>
    </TrangaPage>
</template>

<script setup lang="ts">
import type { GetMangasByMangaIdMetadataRelatedResponse } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';
import type { TrangaPageTitleProps } from '~/components/Tranga/Page.vue';

const mangaId = useRoute().params.mangaId as string;
const { t } = useI18n();

const { data: metadataSources, status: statusMetadata } = await useTranga<GetMangasByMangaIdMetadataRelatedResponse>(
    () => `/mangas/${mangaId}/metadata/related`,
    { key: ApiKeys.Manga.Metadata.RelatedManga(mangaId) },
);

const navigation = computed((): TrangaPageTitleProps => {
    return {
        title: { label: t('manga.nav.manga'), type: 'label' },
        items: [
            { label: t('manga.nav.manga'), to: `/manga/${mangaId}`, icon: 'i-lucide-book' },
            { label: t('manga.nav.metadataEntries'), to: `/manga/${mangaId}/metadataEntries`, icon: 'i-lucide-list' },
            { label: t('manga.nav.tasks'), to: `/tasks?manga=${mangaId}`, icon: 'i-lucide-biceps-flexed' },
            { label: t('common.chapters'), to: `/manga/${mangaId}/chapters`, icon: 'i-lucide-list-checks' },
        ],
    };
});
</script>
