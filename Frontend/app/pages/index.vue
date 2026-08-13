<template>
    <TrangaPage :navigation="navigation">
        <MangaList :mangas="mangaList" :loading="status !== 'success'" />
    </TrangaPage>
</template>

<script setup lang="ts">
import { MangaList } from '#components';
import type { GetLibrariesResponse, GetMangasResponse } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';
import type { TrangaPageTitleProps } from '~/components/Tranga/Page.vue';

const search = ref<string>();

const { data, status, refresh } = await useTranga<GetMangasResponse>('/mangas', { key: ApiKeys.Manga.List });

const mangaList = computed(() =>
    search.value
        ? data.value?.filter((m) => m.metadataEntry?.series.toLocaleLowerCase().includes(search.value!.toLocaleLowerCase()))
        : data.value,
);

defineShortcuts({ shift_r: () => refresh() });

const { data: libraries } = useTranga<GetLibrariesResponse>('/libraries', { key: ApiKeys.Libraries.Libraries });

const navigation = computed((): TrangaPageTitleProps | undefined =>
    libraries.value
        ? {
              title: { label: 'Libraries', type: 'label' },
              items:
                  libraries.value?.map((library) => ({
                      label: library.name,
                      to: library.baseUrl,
                      external: true,
                      icon: 'i-lucide-library',
                  })) ?? [],
          }
        : undefined,
);

onMounted(() => useNuxtApp().$tour.maybeAutoStart());
</script>
