<template>
    <UBlogPosts class="mt-4">
        <USkeleton v-for="_ in [...Array(3)]" v-if="loading" class="w-full h-96" />
        <slot />
    </UBlogPosts>
</template>

<script setup lang="ts">
defineProps<{ loading?: boolean }>();
</script>
