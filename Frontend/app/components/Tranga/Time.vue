<template>
    <UTooltip v-if="date" :text="time">
        <TrangaDoubleBadge :first-badge-props="{ label: prefix }" :second-badge-props="{ label: date.toLocaleString() }" />
    </UTooltip>
</template>

<script setup lang="ts">
import type { TrangaDoubleBadgeProps } from '~/components/Tranga/DoubleBadge.vue';

const time = defineModel<string | null | undefined>();

defineProps<TrangaTimeProps>();

export interface TrangaTimeProps extends TrangaDoubleBadgeProps {
    prefix?: string;
}

const date = computed(() => (time.value ? new Date(time.value) : undefined));
</script>
