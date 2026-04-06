<template>
    <img
        v-if="fileId"
        :src="`http://${useRuntimeConfig().public.api.baseUrl}/files/${fileId}`"
        class="w-full h-full aspect-6/9 object-cover object-center"
        :class="!noBlur && 'blur-md'" />
    <img
        v-else-if="mangaId"
        :src="`http://${useRuntimeConfig().public.api.baseUrl}/mangas/${mangaId}/cover`"
        class="w-full h-full aspect-6/9 object-cover object-center"
        :class="!noBlur && 'blur-md'" />
    <USkeleton v-else class="aspect-6/9" />
</template>

<script setup lang="ts">
defineProps<{ mangaId?: string | null; fileId?: string | null; noBlur?: boolean }>();
</script>
