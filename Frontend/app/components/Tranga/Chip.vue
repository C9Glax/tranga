<template>
    <UChip v-bind="{ ...$props, size: undefined }" :ui="{ base: `py-${sizeToNumber + sizeToPadding} px-${sizeToPadding}` }">
        <template #content>
            <UIcon :name="icon" :class="`size-${sizeToNumber * 2}`" />
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
            return 0.5;
        case '2xs':
            return 1;
        case 'xs':
            return 2;
        case 'sm':
            return 3;
        case 'md':
            return 4;
        case 'lg':
            return 5;
        case 'xl':
            return 6;
        case '2xl':
            return 7;
        case '3xl':
            return 8;
        default:
            return 3;
    }
});

const sizeToPadding = computed(() => {
    if (sizeToNumber.value < 5) return 1;
    else return 2;
});
</script>
