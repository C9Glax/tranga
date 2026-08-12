<template>
    <img v-if="src" :src="src" class="max-h-full w-full aspect-6/9 object-cover object-center" :class="!noBlur && 'blur-md'" />
    <USkeleton v-else class="aspect-6/9" />
</template>

<script setup lang="ts">
const props = defineProps<{ mangaId?: string | null; fileId?: string | null; noBlur?: boolean }>();

const path = computed(() => {
    if (props.fileId) return `/mangas/files/${props.fileId}`;
    if (props.mangaId) return `/mangas/${props.mangaId}/cover`;
    return undefined;
});

const src = useAuthedImageUrl(path);
</script>
