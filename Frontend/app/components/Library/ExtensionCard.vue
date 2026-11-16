<template>
    <UCard>
        <div class="flex flex-row items-center justify-between gap-4">
            <div class="flex flex-col">
                <span class="font-medium">{{ library.libraryServiceType }}</span>
                <span class="text-sm text-muted">{{ library.baseUrl }}</span>
            </div>
            <div class="flex flex-row gap-2">
                <UButton :label="$t('settings.libraries.linkManga')" variant="outline" loading-auto @click="linkManga" />
                <UButton :label="$t('common.delete')" color="error" loading-auto @click="removeLibrary" />
            </div>
        </div>
    </UCard>
</template>

<script setup lang="ts">
import type { ServicesLibrariesLibrary, DeleteLibrariesByLibraryIdResponses, PostLibrariesByLibraryIdLinkResponses } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';

const { library } = defineProps<{ library: ServicesLibrariesLibrary }>();

const { t } = useI18n();
const toast = useToast();
const removeLibrary = async () => {
    await useNuxtApp().$tranga<DeleteLibrariesByLibraryIdResponses>(`/libraries/${library.id}`, {
        method: 'delete',
        onResponse({ response }) {
            if (response.status !== 200) {
                toast.add({ title: t('settings.libraries.toast.removeFailed'), color: 'error' });
                return;
            }
            clearNuxtData(ApiKeys.Libraries.Library(library.id));
            refreshNuxtData(ApiKeys.Libraries.Libraries);
            toast.add({ title: t('settings.libraries.toast.removed'), color: 'success' });
        },
    });
};

const linkManga = async () => {
    await useNuxtApp().$tranga<PostLibrariesByLibraryIdLinkResponses>(`/libraries/${library.id}/link`, {
        method: 'post',
        onResponse({ response }) {
            if (response.status !== 200) {
                toast.add({ title: t('settings.libraries.toast.linkFailed'), color: 'error' });
                return;
            }
            const linkedCount = Number(response._data);
            toast.add({
                title:
                    linkedCount > 0
                        ? t('settings.libraries.toast.linked', { count: linkedCount })
                        : t('settings.libraries.toast.noneLinked'),
                color: 'success',
            });
        },
    });
};
</script>
