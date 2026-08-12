<template>
    <UPopover
        :open="tour.open.value"
        :reference="tour.reference.value"
        :content="{ side: 'bottom', sideOffset: 8 }"
        :dismissible="false"
        arrow>
        <template #content>
            <div class="p-4 max-w-xs space-y-3">
                <div class="flex items-center justify-between gap-4">
                    <p class="font-semibold text-highlighted">{{ title }}</p>
                    <span v-if="isEdgeStep" class="text-xs text-muted tabular-nums"
                        >{{ tour.index.value + 1 }} / {{ tour.total.value }}</span
                    >
                </div>
                <p class="text-sm text-muted">{{ body }}</p>
                <div class="flex items-center justify-end pt-1">
                    <UButton v-if="isEdgeStep" size="sm" @click="onPrimary">{{ tour.hasNext.value ? "Let's go" : 'Finish' }}</UButton>
                    <UButton v-else variant="ghost" color="neutral" size="sm" @click="tour.finish()">Skip tour</UButton>
                </div>
            </div>
        </template>
    </UPopover>
</template>

<script setup lang="ts">
const { tour } = useNuxtApp().$tour;

const title = computed(() => (tour.current.value as { title?: string } | undefined)?.title ?? '');
const body = computed(() => (tour.current.value as { body?: string } | undefined)?.body ?? '');
const isEdgeStep = computed(() => (tour.current.value as { key?: string } | undefined)?.key === undefined);

const onPrimary = (): void => {
    if (tour.hasNext.value) tour.next();
    else tour.finish();
};
</script>
