<template>
    <UAvatar :src="iconSrc" />
</template>

<script setup lang="ts">
const props = defineProps<{ id: string }>();

const iconSrc = computed(() => (icons.has(props.id) ? icons.get(props.id) : '/blahaj.png'));

const icons: Map<string, string> = new Map([
    ['019cf2cb-3aac-7c9c-9580-7091471b6788', 'https://www.mangaupdates.com/images/manga-updates.svg'],
]);
</script>
