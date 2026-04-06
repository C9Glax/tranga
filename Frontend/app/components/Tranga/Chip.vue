<template>
    <UChip v-bind="{ ...$props, size: undefined }" :ui="{ base: `h-${sizeToNumber * 2} w-${sizeToNumber * 2}` }">
        <template #content>
            <UIcon :name="icon" :class="`size-${sizeToNumber}`" />
        </template>
        <slot></slot>
    </UChip>
</template>
<script setup lang="ts">
import type { ChipProps } from '@nuxt/ui/components/Chip.vue';

const props = defineProps<ChipProps & { icon: string }>();

const sizeToNumber = computed((): number => {
    switch (props.size) {
        case '3xs':
            return 3;
        case '2xs':
            return 4;
        case 'xs':
            return 5;
        case 'sm':
            return 7;
        case 'md':
            return 9;
        case 'lg':
            return 11;
        case 'xl':
            return 13;
        case '2xl':
            return 15;
        case '3xl':
            return 18;
        default:
            return 7;
    }
});
</script>
