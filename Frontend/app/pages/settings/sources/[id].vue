<template>
    <TrangaPage>
        <UAlert
            v-if="!definition"
            color="error"
            variant="subtle"
            icon="i-lucide-circle-alert"
            title="Source not found"
            description="This custom source does not exist (anymore)." />
        <CustomSourceForm v-else :initial="definition" submit-label="Save changes" :saving="saving" :errors="errors" @submit="save" />
    </TrangaPage>
</template>

<script setup lang="ts">
import type {
    GetMangasCustomDownloadersByCustomDownloaderIdResponse,
    PutMangasCustomDownloadersByCustomDownloaderIdResponse,
} from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';

const route = useRoute();
const toast = useToast();
const id = String(route.params.id);

const { data } = await useTranga<GetMangasCustomDownloadersByCustomDownloaderIdResponse>(`/mangas/customDownloaders/${id}`, {
    key: ApiKeys.CustomDownloader(id),
});
const definition = computed(() => (data.value ? normalizeCustomSource(data.value) : undefined));

const saving = ref(false);
const errors = ref<string[]>([]);

const save = async (updated: CustomSourceDefinition) => {
    saving.value = true;
    errors.value = [];
    try {
        await useNuxtApp().$tranga<PutMangasCustomDownloadersByCustomDownloaderIdResponse>(`/mangas/customDownloaders/${id}`, {
            method: 'put',
            body: customSourceForApi(updated),
        });
    } catch (error: unknown) {
        errors.value = apiErrorMessages(error, 'Could not save the source.');
        return;
    } finally {
        saving.value = false;
    }

    clearNuxtData(ApiKeys.CustomDownloader(id));
    await refreshNuxtData([ApiKeys.DownloadExtensions, ApiKeys.CustomDownloaders]);
    toast.add({ title: `Saved ${updated.name}.`, color: 'success' });
    await navigateTo('/settings/sources');
};
</script>
