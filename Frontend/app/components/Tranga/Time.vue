<template>
    <UTooltip v-if="date" :text="date.toString()">
        <TrangaDoubleBadge
            :first-badge-props="prefix ? { label: prefix } : undefined"
            :second-badge-props="{ label: relative ? getRelativeTime(date, locale) : date.toLocaleString(locale) }" />
    </UTooltip>
</template>

<script setup lang="ts">
import type { TrangaDoubleBadgeProps } from '~/components/Tranga/DoubleBadge.vue';
import { getRelativeTime } from '~/utils/getRelativeTime';

const { locale } = useI18n();

const time = defineModel<string | Date | null | undefined>();

defineProps<TrangaTimeProps>();

export interface TrangaTimeProps extends /* @vue-ignore */ TrangaDoubleBadgeProps {
    prefix?: string;
    relative?: boolean;
}

const date = computed(() => (time.value ? (time.value instanceof Date ? time.value : new Date(time.value)) : undefined));
</script>
