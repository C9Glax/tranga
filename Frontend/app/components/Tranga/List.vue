<template>
    <div v-if="loading" class="flex flex-row flex-wrap gap-2 justify-center md:justify-start w-full h-full">
        <USkeleton v-for="k in [...Array(3)]" :key="k" class="w-60 h-90" />
    </div>
    <div v-else-if="!empty" class="flex flex-row flex-wrap gap-2 justify-center md:justify-start w-full h-full">
        <slot />
    </div>

    <slot v-else name="empty">
        <UEmpty v-bind="emptyProps" title="There is nothing here..." />
    </slot>
</template>

<script setup lang="ts">
import type { EmptyProps } from '@nuxt/ui/components/Empty.vue';

defineProps<{ loading?: boolean; empty?: boolean; emptyProps?: EmptyProps }>();
</script>
