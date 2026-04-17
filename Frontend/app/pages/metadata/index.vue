<template>
    <TrangaPage v-model:search="search" :page-title="{ title: 'Metadata List', icon: { name: 'i-lucide-info', color: 'info' } }" showSearch>
        <UPageSection :ui="{ container: 'sm:py-0 lg:py-0 gap-8 sm:gap-8' }">
            <MetadataList :metadata-list="metadataList" :loading="status !== 'success'" />
        </UPageSection>
    </TrangaPage>
</template>
<script setup lang="ts">
import type { GetMangasMetadataResponse } from '~/api/tranga';

const search = ref<string>();

const { data, status } = useTranga<GetMangasMetadataResponse>('/mangas/metadata', { key: ApiKeys.MetadataList });

const metadataList = computed(() =>
    search.value ? data.value?.filter((m) => m.series.toLocaleLowerCase().includes(search.value!.toLocaleLowerCase())) : data.value
);
</script>
