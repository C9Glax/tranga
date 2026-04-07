<template>
    <UBlogPosts class="mt-4">
        <USkeleton v-if="loading" v-for="_ in [...Array(3)]" class="w-full h-96" />
        <slot />
    </UBlogPosts>
</template>

<script setup lang="ts">
defineProps<{ loading?: boolean }>();
</script>
