<template>
    <UPageList class="gap-4">
        <UCard>
            <template #header>
                <p class="font-semibold">Content</p>
            </template>

            <UTooltip :disabled="!settings?.allowNsfwLocked" text="Set by the AllowNSFW environment variable and cannot be changed here.">
                <USwitch
                    :model-value="settings?.allowNsfw ?? false"
                    :disabled="!settings || settings.allowNsfwLocked"
                    :loading="saving"
                    label="Allow NSFW content"
                    description="Include NSFW manga in search results and allow NSFW sources."
                    @update:model-value="setAllowNsfw" />
            </UTooltip>
        </UCard>
    </UPageList>
</template>

<script setup lang="ts">
import type { ServicesMangaBehaviorSettings } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';
import { FetchError } from 'ofetch';

const toast = useToast();

const { data: settings, refresh } = useTranga<ServicesMangaBehaviorSettings>('/mangas/settings/behavior', {
    key: ApiKeys.BehaviorSettings,
});

const saving = ref(false);

const setAllowNsfw = async (value: boolean | 'indeterminate') => {
    saving.value = true;
    try {
        await useNuxtApp().$tranga('/mangas/settings/behavior', { method: 'patch', body: { allowNsfw: value === true } });
        // Cached source/search data was filtered with the previous value.
        await refreshNuxtData([ApiKeys.DownloadExtensions, ApiKeys.MetadataExtensions]);
    } catch (error: unknown) {
        const description = error instanceof FetchError ? (error.data ?? error.message) : 'Could not update the setting.';
        toast.add({ title: 'Failed to update setting.', description, color: 'error' });
    } finally {
        await refresh();
        saving.value = false;
    }
};
</script>
