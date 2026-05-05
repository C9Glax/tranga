<template>
    <UFieldGroup>
        <UBadge v-if="firstBadgeProps" v-bind="{ ...$props, ...firstBadgeProps }" />
        <UBadge v-bind="{ ...$props, variant: 'outline', ...secondBadgeProps }" />
    </UFieldGroup>
</template>

<script setup lang="ts">
import type { BadgeProps } from '@nuxt/ui';

defineProps<TrangaDoubleBadgeProps>();

export interface TrangaDoubleBadgeProps extends BadgeProps {
    firstBadgeProps?: BadgeProps;
    secondBadgeProps?: BadgeProps;
}
</script>
