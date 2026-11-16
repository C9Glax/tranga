<template>
    <UPageList class="gap-4">
        <UCard>
            <template #header>
                <p class="font-semibold">{{ $t('settings.behavior.content') }}</p>
            </template>

            <UTooltip :disabled="!settings?.allowNsfwLocked" :text="$t('settings.behavior.allowNsfwLocked')">
                <USwitch
                    :model-value="settings?.allowNsfw ?? false"
                    :disabled="!settings || settings.allowNsfwLocked"
                    :loading="saving"
                    :label="$t('settings.behavior.allowNsfw.label')"
                    :description="$t('settings.behavior.allowNsfw.description')"
                    @update:model-value="setAllowNsfw" />
            </UTooltip>
        </UCard>
    </UPageList>
</template>

<script setup lang="ts">
import type { ServicesMangaBehaviorSettings } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';
import { FetchError } from 'ofetch';

const { t } = useI18n();
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
        const description = error instanceof FetchError ? (error.data ?? error.message) : t('settings.behavior.updateFailedFallback');
        toast.add({ title: t('settings.behavior.updateFailed'), description, color: 'error' });
    } finally {
        await refresh();
        saving.value = false;
    }
};
</script>
