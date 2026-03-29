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
                    @keyup.enter="search">
                    <template #trailing>
                        <UIcon class="size-7" name="i-lucide-arrow-right" @click="search" />
                    </template>
                </UInput>
                <MangaList :items="searchResult" />
            </div>
        </template>
    </UModal>
</template>

<script setup lang="ts">
import { MangaList } from '#components';

const searchTerm = ref<string>();
const searchResult = ref();

const search = async () => {
    const { data } = await api.POST('/manga/search', { body: { title: searchTerm.value } });
    searchResult.value = data;
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
