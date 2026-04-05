<template>
    <UApp>
        <UHeader :toggle="true">
            <template #left>
                <NuxtLink to="/">
                    <div class="h-full flex gap-2 items-center">
                        <img src="/blahaj.png" class="h-lh cursor-grab" alt="Blahaj" />
                        <p
                            style="
                                background: linear-gradient(110deg, var(--color-pink), var(--color-blue));
                                background-clip: text;
                                -webkit-background-clip: text;
                                -webkit-text-fill-color: transparent;
                            "
                            class="font-bold cursor-pointer text-3xl">
                            Tranga
                        </p>
                    </div>
                </NuxtLink>
            </template>
            <template #right>
                <UButton icon="i-lucide-book-search" @click="searchOverlay.open()">Search <UKbd value="ctrl+k" /></UButton>
            </template>
        </UHeader>
        <NuxtLayout>
            <UButton @click="$router.back()" icon="i-lucide-arrow-left" variant="soft" class="m-4">Back</UButton>
            <UContainer class="my-4">
                <NuxtPage />
            </UContainer>
        </NuxtLayout>
    </UApp>
</template>

<script setup>
import { LazySearch } from '#components';

const overlay = useOverlay();

const searchOverlay = overlay.create(LazySearch);

defineShortcuts({ ctrl_k: () => searchOverlay.open() });
</script>
