<template>
    <TrangaPage>
        <UEmpty
            v-if="!definition"
            icon="i-lucide-globe"
            :title="$t('customSource.sources.notFound')"
            :description="$t('customSource.sources.notFoundDescription')"
            variant="naked"
            :actions="[
                {
                    label: $t('customSource.sources.backToSources'),
                    icon: 'i-lucide-arrow-left',
                    color: 'neutral',
                    variant: 'subtle',
                    to: '/settings/sources',
                },
            ]" />
        <CustomSourceForm
            v-else
            :initial="definition"
            :submit-label="$t('customSource.sources.saveSubmit')"
            :saving="saving"
            :errors="errors"
            @submit="save" />
    </TrangaPage>
</template>

<script setup lang="ts">
import type {
    GetMangasCustomDownloadersByCustomDownloaderIdResponse,
    PutMangasCustomDownloadersByCustomDownloaderIdResponse,
} from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';

const route = useRoute();
const { t } = useI18n();
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
        errors.value = apiErrorMessages(error, t('customSource.errors.saveFailed'));
        return;
    } finally {
        saving.value = false;
    }

    clearNuxtData(ApiKeys.CustomDownloader(id));
    await refreshNuxtData([ApiKeys.DownloadExtensions, ApiKeys.CustomDownloaders]);
    toast.add({ title: t('customSource.sources.saved', { name: updated.name }), color: 'success' });
    await navigateTo('/settings/sources');
};
</script>
