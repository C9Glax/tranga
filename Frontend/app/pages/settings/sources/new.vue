<template>
    <TrangaPage>
        <CustomSourceForm
            :initial="initial"
            :submit-label="$t('customSource.sources.createSubmit')"
            :saving="saving"
            :errors="errors"
            show-examples
            @submit="create" />
    </TrangaPage>
</template>

<script setup lang="ts">
import type { PostMangasCustomDownloadersResponse } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';

const { t } = useI18n();
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
        errors.value = apiErrorMessages(error, t('customSource.errors.createFailed'));
        return;
    } finally {
        saving.value = false;
    }

    await refreshNuxtData([ApiKeys.DownloadExtensions, ApiKeys.CustomDownloaders]);
    toast.add({
        title: t('customSource.sources.created', { name: definition.name }),
        description: t('customSource.sources.createdDescription'),
        color: 'success',
    });
    await navigateTo('/settings/sources');
};
</script>
