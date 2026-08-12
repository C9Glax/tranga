<template>
    <img v-if="src" :src="src" class="max-h-full w-full aspect-6/9 object-cover object-center" :class="!noBlur && 'blur-md'" />
    <USkeleton v-else class="aspect-6/9" />
</template>

<script setup lang="ts">
const props = defineProps<{ mangaId?: string | null; fileId?: string | null; noBlur?: boolean; size: CoverSize }>();

const path = computed(() => {
    let base: string | undefined;
    if (props.fileId) base = `/mangas/files/${props.fileId}`;
    else if (props.mangaId) base = `/mangas/${props.mangaId}/cover`;
    return base ? withCoverSize(base, props.size) : undefined;
});

const src = useAuthedImageUrl(path);
</script>
