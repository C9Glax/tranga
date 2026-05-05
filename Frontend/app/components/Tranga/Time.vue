<template>
    <UTooltip v-if="date" :text="date.toString()">
        <TrangaDoubleBadge
            :first-badge-props="{ label: prefix }"
            :second-badge-props="{ label: relative ? getRelativeTime(date) : date.toLocaleString() }" />
    </UTooltip>
</template>

<script setup lang="ts">
import type { TrangaDoubleBadgeProps } from '~/components/Tranga/DoubleBadge.vue';
import { getRelativeTime } from '~/utils/getRelativeTime';

const time = defineModel<string | Date | null | undefined>();

defineProps<TrangaTimeProps>();

export interface TrangaTimeProps extends TrangaDoubleBadgeProps {
    prefix?: string;
    relative?: boolean;
}

const date = computed(() => (time.value ? (time instanceof Date ? time : new Date(time.value)) : undefined));
</script>
