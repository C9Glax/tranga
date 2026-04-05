<template>
    <UModal fullscreen class="py-12">
        <template #content>
            <div class="flex flex-col gap-2 px-4 sm:px-4 lg:px-12 xl:px-32 2xl:px-48">
                <UInput
                    v-model="searchTerm"
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
                <MangaList v-if="searchResult || loading" :mangas="searchResult" :loading="loading" />
            </div>
        </template>
    </UModal>
</template>

<script setup lang="ts">
import type { MangaMetadata, PostMangasSearchResponse } from '~/api/trangaApi';

const searchTerm = ref<string>();
const searchResult = ref<MangaMetadata[]>();

const loading = ref<boolean>(false);

const toast = useToast();

const search = async () => {
    try {
        loading.value = true;
        const { data } = await useTranga<PostMangasSearchResponse>('/mangas/search', { body: { title: searchTerm.value }, method: 'POST' });
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
