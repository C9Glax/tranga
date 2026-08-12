<template>
    <UApp>
        <NuxtLayout>
            <NuxtPage />
        </NuxtLayout>
        <ClientOnly>
            <TourGuide />
        </ClientOnly>
    </UApp>
</template>

<script setup lang="ts">
import { TourGuide } from '#components';
</script>
