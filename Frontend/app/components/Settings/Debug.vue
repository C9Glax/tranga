<template>
    <UPageList class="gap-4">
        <UCard>
            <template #header>
                <p class="font-semibold">{{ $t('settings.debug.about') }}</p>
            </template>

            <SettingsAbout />
        </UCard>

        <UCard>
            <template #header>
                <p class="font-semibold">{{ $t('settings.debug.maintenance') }}</p>
            </template>

            <div class="flex flex-col items-start gap-2">
                <UButton :label="$t('settings.debug.replayTour')" icon="i-lucide-wand-sparkles" variant="outline" @click="replayTour" />
                <UButton
                    :label="$t('settings.debug.backfillComicInfo')"
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

const { t } = useI18n();
const toast = useToast();

const replayTour = () => useNuxtApp().$tour.restart();

const backfillComicInfo = async () => {
    try {
        await useNuxtApp().$tranga<PutTasksCreateBackfillComicInfoResponses>('/tasks/create/backfillComicInfo', { method: 'put' });
    } catch (error: unknown) {
        const description = error instanceof FetchError ? (error.data ?? error.message) : t('settings.debug.backfillFailedFallback');
        toast.add({ title: t('settings.debug.backfillFailed'), description, color: 'error' });
        return;
    }

    toast.add({
        title: t('settings.debug.backfillCreated'),
        description: t('settings.debug.backfillCreatedDescription'),
        color: 'success',
    });
};
</script>
