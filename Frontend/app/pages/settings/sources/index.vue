<template>
    <TrangaPage>
        <UPageList class="gap-2">
            <div class="flex flex-row flex-wrap items-center justify-between gap-2">
                <p class="text-sm text-muted">
                    {{ $t('customSource.sources.description') }}
                </p>
                <UButton :label="$t('customSource.sources.add')" icon="i-lucide-plus" to="/settings/sources/new" />
            </div>

            <UPageCard v-for="extension in downloadExtensions" :key="extension.downloadExtensionsId" variant="subtle">
                <div class="flex flex-row items-center justify-between gap-3">
                    <div class="flex min-w-0 flex-row items-center gap-3">
                        <img
                            v-if="!failedIcons.has(extension.downloadExtensionsId)"
                            :src="extension.iconUrl"
                            :alt="extension.name"
                            class="size-8 shrink-0 rounded"
                            @error="failedIcons.add(extension.downloadExtensionsId)" />
                        <UIcon v-else name="i-lucide-globe" class="size-8 shrink-0 text-muted" />
                        <span class="truncate font-medium">{{ extension.name }}</span>
                        <UBadge v-if="extension.isCustom" :label="$t('customSource.sources.custom')" color="primary" variant="subtle" />
                        <UBadge v-else :label="$t('customSource.sources.builtIn')" color="neutral" variant="outline" />
                    </div>
                    <div v-if="extension.isCustom" class="flex flex-row gap-2">
                        <UButton
                            :label="$t('common.edit')"
                            icon="i-lucide-pencil"
                            color="neutral"
                            variant="subtle"
                            size="sm"
                            :to="`/settings/sources/${extension.downloadExtensionsId}`" />
                        <UButton
                            :label="$t('common.delete')"
                            icon="i-lucide-trash-2"
                            color="error"
                            variant="soft"
                            size="sm"
                            @click="confirmDelete(extension)" />
                    </div>
                </div>
            </UPageCard>
        </UPageList>
    </TrangaPage>
</template>

<script setup lang="ts">
import { TrangaConfirmModal } from '#components';
import { FetchError } from 'ofetch';
import type { ServicesMangaDownloadExtension } from '~/api/tranga';
import useDownloadExtensions from '~/composables/DownloadExtension';
import { ApiKeys } from '~/composables/ApiKeys';

const { t } = useI18n();
const toast = useToast();
const confirmModal = useOverlay().create(TrangaConfirmModal);

const { downloadExtensions } = await useDownloadExtensions();

// Icons come from other websites and can fail to load; those sources get a generic icon instead.
const failedIcons = reactive(new Set<string>());

const confirmDelete = (extension: ServicesMangaDownloadExtension) =>
    confirmModal.open({
        title: t('customSource.sources.deleteTitle', { name: extension.name }),
        description: t('customSource.sources.deleteDescription'),
        onConfirm: async () => {
            try {
                await useNuxtApp().$tranga(`/mangas/customDownloaders/${extension.downloadExtensionsId}`, { method: 'delete' });
            } catch (error: unknown) {
                toast.add({
                    title: t('customSource.sources.deleteFailed'),
                    description: error instanceof FetchError ? (error.data ?? error.message) : undefined,
                    color: 'error',
                });
                return;
            }
            clearNuxtData(ApiKeys.CustomDownloader(extension.downloadExtensionsId));
            await refreshNuxtData([ApiKeys.DownloadExtensions, ApiKeys.CustomDownloaders]);
            toast.add({ title: t('customSource.sources.deleted', { name: extension.name }), color: 'success' });
        },
    });
</script>
