<template>
    <ClientOnly>
        <UDashboardSearch
            v-bind="$props"
            v-model:search-term="searchTerm"
            :color-mode="false"
            :placeholder="`Search for... (${randomManga}?)`"
            :groups="displayManga"
            :ui="{ modal: 'sm:h-auto max-h-full' }"
            :loading="loading"
            loading-icon="i-lucide-loader-pinwheel"
            virtualize
            close
            overlay
            dismissible
            :search-delay="0"
            @update:search-term="debounceSearch">
            <template #item-leading="{ item }">
                <MangaCover
                    :file-id="item?.fileId"
                    :manga-id="item?.mangaId"
                    :no-blur="!item?.metadataEntry?.nsfw"
                    :size="COVER_SIZES.tiny"
                    class="aspect-6/9 max-h-6 max-w-9" />
            </template>
            <template #footer>
                <UProgress v-if="!loading && !debounceTimeout" :model-value="0" />
                <UProgress v-else-if="loading" animation="swing" />
                <UProgress v-else-if="!loading" :max="DEBOUNCE_TIME" :model-value="progressValue" inverted />
                <UProgress v-else :model-value="100" />
                <UCheckboxGroup
                    v-model="selectedExtensions"
                    legend="Search on:"
                    orientation="horizontal"
                    color="secondary"
                    :items="metadataExtensions.extensions"
                    label-key="name"
                    value-key="metadataExtensionId" />
            </template>
        </UDashboardSearch>
    </ClientOnly>
</template>

<script setup lang="ts">
import type { ServicesMangaIMetadataExtension, GetMangasResponse, PostMangasSearchResponse, ServicesMangaMetadata } from '~/api/tranga';
import useMetadataExtensions from '~/composables/MetadataExtension';
import { ApiKeys } from '~/composables/ApiKeys';
import type { CommandPaletteGroup, CommandPaletteItem } from '@nuxt/ui/components/CommandPalette.vue';
import { MangaCover } from '#components';
import type { DashboardSearchProps } from '@nuxt/ui/components/DashboardSearch.vue';

defineProps<DashboardSearchProps>();

const searchTerm = ref<string>();

const { metadataExtensions } = await useMetadataExtensions();
watch(
    metadataExtensions,
    () =>
        (selectedExtensions.value =
            metadataExtensions.value.extensions?.map((e: ServicesMangaIMetadataExtension) => e.metadataExtensionId as string) ?? []),
);
const selectedExtensions = ref<string[]>(
    metadataExtensions.value.extensions?.map((e: ServicesMangaIMetadataExtension) => e.metadataExtensionId as string) ?? [],
);

const displayManga = computed((): CommandPaletteGroup<CommandPaletteItem>[] => [...matchedManga.value, ...searchResultItems.value]);

interface MyCommandPaletteItem extends CommandPaletteItem {
    fileId?: string | null;
    mangaId?: string | null;
}

const matchedManga = computed((): CommandPaletteGroup<MyCommandPaletteItem>[] => [
    {
        id: 'localResults',
        label: 'Local Manga',
        items: data.value?.map((manga) => ({
            label: manga.metadataEntry?.series,
            description: manga.metadataEntry?.summary ?? undefined,
            suffix: metadataExtensions.value.getExtension(manga.metadataEntry?.metadataExtensionId)?.name ?? undefined,
            onSelect: () => navigateTo(`/manga/${manga.mangaId}`),
            fileId: manga.metadataEntry?.coverId,
            mangaId: manga.mangaId,
        })),
    },
]);
const { data } = await useTranga<GetMangasResponse>('/mangas', { key: ApiKeys.Manga.List });

const loading = ref(false);
const searchResult = ref<ServicesMangaMetadata[]>();
const searchResultItems = computed((): CommandPaletteGroup<MyCommandPaletteItem>[] => [
    {
        id: 'searchResults',
        label: 'Search Results',
        items: searchResult.value?.map((manga) => ({
            label: manga?.series,
            description: manga?.summary ?? undefined,
            suffix: metadataExtensions.value.getExtension(manga?.metadataExtensionId)?.name ?? undefined,
            onSelect: () => navigateTo(`/metadata/${manga.metadataId}`),
            fileId: manga.coverId,
        })),
    },
]);
const search = async (title: string) => {
    try {
        loading.value = true;
        const { data } = await useTranga<PostMangasSearchResponse>('/mangas/search', {
            body: { searchQuery: { title }, metadataExtensionIds: selectedExtensions.value },
            method: 'POST',
        });
        await refreshNuxtData([ApiKeys.Manga.Metadata.List, ApiKeys.Manga.List]);
        searchResult.value = data.value;
    } finally {
        loading.value = false;
    }
};

const DEBOUNCE_TIME = 1000;
const countDown = ref();
const progressValue = ref(DEBOUNCE_TIME);
const debounceTimeout = ref();
const debounceSearch = (searchTerm?: string) => {
    clearTimeout(debounceTimeout.value);
    clearInterval(countDown.value);
    progressValue.value = DEBOUNCE_TIME;
    debounceTimeout.value = setTimeout(() => {
        if (searchTerm) search(searchTerm);
    }, DEBOUNCE_TIME);
    countDown.value = setInterval(() => {
        if (progressValue.value < 15) {
            clearInterval(countDown.value);
            progressValue.value = 0;
            return;
        }
        progressValue.value -= 15; // seems like its pretty slow to do this
    }, 10);
};

const randomManga = computed(() => placeholders[Math.floor(Math.random() * placeholders.length)]!);
const placeholders = [
    'Berserk',
    'One Piece',
    'Haikyuu!!',
    '86: Eighty Six',
    'Sousou no Frieren',
    'Destiny Unchain Online',
    'Kumo desu ga, Nani ka?',
];
</script>
