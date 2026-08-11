<template>
    <UPopover :open="open" :reference="reference" :dismissible="false" :content="{ side: 'bottom' }">
        <template #content>
            <div class="p-4 flex flex-col gap-3 max-w-xs">
                <p class="font-semibold">{{ current?.title }}</p>
                <p class="text-sm text-muted">{{ current?.body }}</p>
                <div class="flex items-center justify-between gap-2">
                    <UButton variant="link" color="neutral" size="xs" label="Skip tour" @click="finishAndPersist" />
                    <UButton v-if="index === 0" label="Let's go" size="xs" @click="next" />
                </div>
            </div>
        </template>
    </UPopover>
</template>

<script setup lang="ts">
const { tour, finishAndPersist, startIfFirstVisit } = useAppTour();
const { open, index, current, reference, next } = tour;

const route = useRoute();

onMounted(() => {
    startIfFirstVisit();
});

watch(
    () => route.name,
    (name) => {
        if (!open.value) return;
        if (name === 'metadata-metadataId' && index.value < 2) {
            tour.goTo(2);
        } else if (name === 'manga-mangaId' && index.value < 3) {
            tour.goTo(3);
        }
    },
);
</script>
