<template>
    <UPageList class="gap-4">
        <UCard>
            <template #header>
                <p class="font-semibold">About</p>
            </template>

            <SettingsAbout />
        </UCard>

        <UCard>
            <template #header>
                <p class="font-semibold">Maintenance</p>
            </template>

            <div class="flex flex-col items-start gap-2">
                <UButton label="Replay first-download tour" icon="i-lucide-wand-sparkles" variant="outline" @click="replayTour" />
                <UButton
                    label="Backfill ComicInfo for downloaded chapters"
                    icon="i-lucide-file-cog"
                    variant="outline"
                    loading-auto
                    @click="backfillComicInfo" />
            </div>
        </UCard>
    </UPageList>
</template>

<script setup lang="ts">
import type { PutTasksCreateBackfillComicInfoResponses } from '~/api/tranga';
import { FetchError } from 'ofetch';

const toast = useToast();

const replayTour = () => useNuxtApp().$tour.restart();

const backfillComicInfo = async () => {
    try {
        await useNuxtApp().$tranga<PutTasksCreateBackfillComicInfoResponses>('/tasks/create/backfillComicInfo', { method: 'put' });
    } catch (error: unknown) {
        const description = error instanceof FetchError ? (error.data ?? error.message) : 'Could not start the backfill task.';
        toast.add({ title: 'Failed to start backfill.', description, color: 'error' });
        return;
    }

    toast.add({ title: 'Backfill task created.', description: 'Check the Tasks page for progress.', color: 'success' });
};
</script>
