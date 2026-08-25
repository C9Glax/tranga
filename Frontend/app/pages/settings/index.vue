<template>
    <TrangaPage>
        <UPageList>
            <UButton to="/settings/sources" label="Sources" icon="i-lucide-globe" />
            <UButton to="/settings/notifications" label="Notifications" icon="i-lucide-megaphone" />
            <UButton to="/settings/libraries" label="Libraries" icon="i-lucide-library" />
            <UButton to="/settings/security" label="Security" icon="i-lucide-key-round" />
            <UButton to="/settings/about" label="About" icon="i-lucide-info" />
            <UButton label="Replay first-download tour" icon="i-lucide-wand-sparkles" variant="outline" @click="replayTour" />
            <UButton
                label="Backfill ComicInfo for downloaded chapters"
                icon="i-lucide-file-cog"
                variant="outline"
                loading-auto
                @click="backfillComicInfo" />
        </UPageList>
    </TrangaPage>
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
