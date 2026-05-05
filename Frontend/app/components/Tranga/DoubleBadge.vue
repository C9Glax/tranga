<template>
    <UFieldGroup>
        <UBadge v-if="firstBadgeProps" v-bind="firstBadgeProps" />
        <UBadge v-bind="{ variant: 'outline', ...secondBadgeProps }" />
    </UFieldGroup>
</template>

<script setup lang="ts">
import type { BadgeProps } from '@nuxt/ui';

defineProps<TrangaDoubleBadgeProps>();

export interface TrangaDoubleBadgeProps {
    firstBadgeProps?: BadgeProps;
    secondBadgeProps?: BadgeProps;
}
</script>
