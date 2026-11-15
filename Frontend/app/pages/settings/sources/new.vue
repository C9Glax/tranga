<template>
    <TrangaPage>
        <CustomSourceForm
            :initial="initial"
            submit-label="Create source"
            :saving="saving"
            :errors="errors"
            show-examples
            @submit="create" />
    </TrangaPage>
</template>

<script setup lang="ts">
import { FetchError } from 'ofetch';
import type { PostMangasCustomDownloadersResponse } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';

const toast = useToast();

const initial = emptyCustomSource();
const saving = ref(false);
const errors = ref<string[]>([]);

const create = async (definition: CustomSourceDefinition) => {
    saving.value = true;
    errors.value = [];
    try {
        await useNuxtApp().$tranga<PostMangasCustomDownloadersResponse>('/mangas/customDownloaders', {
            method: 'post',
            body: customSourceForApi(definition),
        });
    } catch (error: unknown) {
        // 400: the API lists everything that is wrong with the definition
        errors.value =
            error instanceof FetchError && Array.isArray(error.data)
                ? (error.data as string[])
                : [error instanceof FetchError && typeof error.data === 'string' ? error.data : 'Could not create the source.'];
        return;
    } finally {
        saving.value = false;
    }

    await refreshNuxtData([ApiKeys.DownloadExtensions, ApiKeys.CustomDownloaders]);
    toast.add({
        title: `Created ${definition.name}.`,
        description: 'It is available as a download source straight away.',
        color: 'success',
    });
    await navigateTo('/settings/sources');
};
</script>
