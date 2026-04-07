<template>
    <UModal fullscreen class="py-12">
        <template #content>
            <div class="flex flex-col gap-2 px-4 sm:px-4 lg:px-12 xl:px-32 2xl:px-48 h-full">
                <div class="flex flex-col gap-2">
                    <UInput
                        v-model="searchQuery.title"
                        :placeholder="`${placeholders[Math.floor(Math.random() * placeholders.length)]} ...`"
                        size="xl"
                        :ui="{ base: 'px-4 py-3 text-3xl!' }"
                        class="w-full"
                        :loading="loading"
                        :disabled="loading"
                        @keyup.enter="search">
                        <template #trailing>
                            <UIcon class="size-7" name="i-lucide-arrow-right" @click="search" />
                        </template>
                    </UInput>
                    <UCheckboxGroup
                        legend="Search on:"
                        v-model="selectedExtensions"
                        orientation="horizontal"
                        color="secondary"
                        :items="metadataExtensions"
                        label-key="name"
                        value-key="metadataExtensionId" />
                </div>
                <UContainer class="overflow-y-auto pt-4 pb-1">
                    <MetadataList v-if="searchResult || loading" :metadata-list="searchResult" :loading="loading" />
                </UContainer>
            </div>
        </template>
    </UModal>
</template>

<script setup lang="ts">
import type { IMetadataExtension, Metadata, PostMangasSearchResponse } from '~/api/trangaApi';
import useMetadataExtensions from '~/composables/MetadataExtension';

const searchResult = ref<Metadata[]>();

const loading = ref<boolean>(false);

const toast = useToast();
const { metadataExtensions } = await useMetadataExtensions();

const selectedExtensions = ref<string[]>(metadataExtensions.value?.map((e: IMetadataExtension) => e.metadataExtensionId as string) ?? []);
const searchQuery = ref<{ title?: string }>({});

const search = async () => {
    try {
        loading.value = true;
        searchResult.value = undefined;
        const { data } = await useTranga<PostMangasSearchResponse>('/mangas/search', {
            body: { searchQuery: searchQuery, metadataExtensionIds: selectedExtensions.value },
            method: 'POST',
        });
        await refreshNuxtData([ApiKeys.MetadataList, ApiKeys.MangaList]);
        searchResult.value = data.value;
    } catch {
        toast.add({ title: 'Failed to search manga!', color: 'error' });
    } finally {
        loading.value = false;
    }
};

const placeholders = [
    'Berserk',
    'One Piece',
    'Haikyuu!!',
    '86: Eighty Six',
    'Sousou no Frieren',
    'Destiny Unchain Online',
    'Kumo desu ga, Nani ka?',
];

defineEmits(['close']);
</script>
