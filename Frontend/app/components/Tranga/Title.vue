<template>
    <UTooltip v-if="isTruncated" :text="title ?? ''" :ui="{ content: 'max-w-xs' }">
        <component :is="as">{{ displayText }}</component>
    </UTooltip>
    <component :is="as" v-else>{{ displayText }}</component>
</template>

<script setup lang="ts">
const props = withDefaults(defineProps<{ title?: string | null; maxLength?: number; as?: string }>(), { maxLength: 28, as: 'span' });

const isTruncated = computed(() => (props.title?.length ?? 0) > props.maxLength);
const displayText = computed(() => (isTruncated.value ? `${(props.title ?? '').slice(0, props.maxLength - 1)}…` : (props.title ?? '')));
</script>
