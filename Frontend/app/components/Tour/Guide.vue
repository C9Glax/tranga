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
                <div class="flex items-center pt-1" :class="showSkip && showPrimary ? 'justify-between' : 'justify-end'">
                    <UButton v-if="showSkip" variant="ghost" color="neutral" size="sm" @click="tour.finish()">Skip tour</UButton>
                    <UButton v-if="showPrimary" size="sm" @click="onPrimary">{{ primaryLabel }}</UButton>
                </div>
            </div>
        </template>
    </UPopover>
</template>

<script setup lang="ts">
const { tour } = useNuxtApp().$tour;

type CurrentStep = { title?: string; body?: string; key?: string; manualNext?: boolean } | undefined;
const current = computed(() => tour.current.value as CurrentStep);

const title = computed(() => current.value?.title ?? '');
const body = computed(() => current.value?.body ?? '');
const isEdgeStep = computed(() => current.value?.key === undefined);

const showSkip = computed(() => !isEdgeStep.value);
const showPrimary = computed(() => isEdgeStep.value || current.value?.manualNext === true);
const primaryLabel = computed(() => {
    if (!isEdgeStep.value) return 'Next step';
    return tour.hasNext.value ? "Let's go" : 'Finish';
});

const onPrimary = (): void => {
    if (tour.hasNext.value) tour.next();
    else tour.finish();
};
</script>
