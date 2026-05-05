<template>
    <UTooltip v-if="date" :text="time">
        <UFieldGroup>
            <UBadge v-if="prefix">{{ prefix }}</UBadge>
            <UBadge v-bind="$props" :label="date.toLocaleString()" variant="outline" />
        </UFieldGroup>
    </UTooltip>
</template>

<script setup lang="ts">
import type { BadgeProps } from '@nuxt/ui';

const time = defineModel<string | null | undefined>();

defineProps<TrangaTimeProps>();

export interface TrangaTimeProps extends /** @vue-ignore */ BadgeProps {
    prefix?: string;
}

const date = computed(() => (time.value ? new Date(time.value) : undefined));
</script>
