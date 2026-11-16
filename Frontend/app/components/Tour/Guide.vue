<template>
    <UPopover :open="visible" :reference="tour.reference.value" :content="{ side: 'bottom', sideOffset: 8 }" :dismissible="false" arrow>
        <template #content>
            <div class="p-4 max-w-xs space-y-3">
                <div class="flex items-center justify-between gap-4">
                    <p class="font-semibold text-highlighted">{{ title }}</p>
                    <span v-if="isEdgeStep" class="text-xs text-muted tabular-nums"
                        >{{ tour.index.value + 1 }} / {{ tour.total.value }}</span
                    >
                </div>
                <p class="text-sm text-muted">{{ body }}</p>
                <div class="flex items-center justify-between gap-2 pt-1">
                    <div class="flex items-center gap-1">
                        <UButton v-if="canGoBack" variant="ghost" color="neutral" size="sm" icon="i-lucide-arrow-left" @click="goBack()">
                            {{ $t('common.back') }}
                        </UButton>
                        <UButton v-if="showSkip" variant="ghost" color="neutral" size="sm" @click="tour.finish()">{{ $t('tour.skip') }}</UButton>
                    </div>
                    <UButton v-if="showPrimary" size="sm" @click="onPrimary">{{ primaryLabel }}</UButton>
                </div>
            </div>
        </template>
    </UPopover>

    <!-- While a step waits for its element (e.g. the result the user still has to pick) there is nothing to point at, so no
         popover is shown. This chip keeps the tour dismissible meanwhile. -->
    <UButton
        v-if="tour.open.value && !visible"
        :label="$t('tour.skip')"
        icon="i-lucide-x"
        color="neutral"
        variant="subtle"
        size="xs"
        class="fixed right-4 bottom-4 z-[100] shadow-md"
        @click="tour.finish()" />
</template>

<script setup lang="ts">
const { t } = useI18n();
const { tour, canGoBack, goBack } = useNuxtApp().$tour;

type CurrentStep = { title?: string; body?: string; key?: string; manualNext?: boolean } | undefined;
const current = computed(() => tour.current.value as CurrentStep);

// The steps hold i18n keys (see plugins/tour.ts).
const title = computed(() => (current.value?.title ? t(current.value.title) : ''));
const body = computed(() => (current.value?.body ? t(current.value.body) : ''));
const isEdgeStep = computed(() => current.value?.key === undefined);

// A step whose element does not exist yet resolves to the center of the screen (the composable cannot tell "no target on
// purpose" from "not there yet"), which put the popover underneath the search overlay. Only intro / outro steps may float
// there when they have nothing to point at; every other step waits until its element is on the page.
const anchored = computed(() => tour.reference.value instanceof Element);
const visible = computed(() => tour.open.value && (anchored.value || isEdgeStep.value));

const showSkip = computed(() => !isEdgeStep.value);
const showPrimary = computed(() => isEdgeStep.value || current.value?.manualNext === true);
const primaryLabel = computed(() => {
    if (!isEdgeStep.value) return t('tour.nextStep');
    return tour.hasNext.value ? t('tour.letsGo') : t('tour.finish');
});

const onPrimary = (): void => {
    if (tour.hasNext.value) tour.next();
    else tour.finish();
};
</script>
