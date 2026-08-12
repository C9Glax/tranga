<template>
    <UPopover :open="open" :reference="reference" :dismissible="false" :content="{ side: 'bottom' }">
        <template #content>
            <div class="p-4 flex flex-col gap-3 max-w-xs">
                <p class="font-semibold">{{ current?.title }}</p>
                <p class="text-sm text-muted">{{ current?.body }}</p>
                <div class="flex items-center justify-between gap-2">
                    <UButton variant="link" color="neutral" size="xs" label="Skip tour" @click="finishAndPersist" />
                </div>
            </div>
        </template>
    </UPopover>
</template>

<script setup lang="ts">
const { tour, finishAndPersist, startIfFirstVisit } = useAppTour();
const { open, index, current, reference } = tour;

const route = useRoute();

function advanceForRoute(name: string | symbol | null | undefined): void {
    if (!open.value) return;
    if (name === 'metadata-metadataId' && index.value < 1) {
        tour.goTo(1);
    } else if (name === 'manga-mangaId' && index.value < 2) {
        tour.goTo(2);
    }
}

onMounted(() => {
    startIfFirstVisit();
    // Covers landing directly on a mid-flow route (deep link, or a reload
    // mid-tour) — `watch` below only fires on subsequent route changes.
    advanceForRoute(route.name);
});

watch(() => route.name, advanceForRoute);
</script>
