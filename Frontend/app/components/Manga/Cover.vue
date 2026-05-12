<template>
    <img
        v-if="fileId"
        :src="`${$apiBaseUrl}/mangas/files/${fileId}`"
        class="max-h-full w-full aspect-6/9 object-cover object-center"
        :class="!noBlur && 'blur-md'" />
    <img
        v-else-if="mangaId"
        :src="`${$apiBaseUrl}/mangas/${mangaId}/cover`"
        class="max-h-full w-full aspect-6/9 object-cover object-center"
        :class="!noBlur && 'blur-md'" />
    <USkeleton v-else class="aspect-6/9" />
</template>

<script setup lang="ts">
defineProps<{ mangaId?: string | null; fileId?: string | null; noBlur?: boolean }>();
</script>
