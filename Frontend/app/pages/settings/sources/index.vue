<template>
    <TrangaPage>
        <UPageList class="gap-2">
            <div class="flex flex-row flex-wrap items-center justify-between gap-2">
                <p class="text-sm text-muted">
                    Tranga searches all of these when looking for a manga to download. Websites that are not built in can be added as custom
                    sources.
                </p>
                <UButton label="Add custom source" icon="i-lucide-plus" to="/settings/sources/new" />
            </div>

            <UPageCard v-for="extension in downloadExtensions" :key="extension.downloadExtensionsId" variant="subtle">
                <div class="flex flex-row items-center justify-between gap-3">
                    <div class="flex min-w-0 flex-row items-center gap-3">
                        <img :src="extension.iconUrl" :alt="extension.name" class="size-8 shrink-0 rounded" />
                        <span class="truncate font-medium">{{ extension.name }}</span>
                        <UBadge v-if="extension.isCustom" label="Custom" color="primary" variant="subtle" />
                        <UBadge v-else label="Built-in" color="neutral" variant="outline" />
                    </div>
                    <div v-if="extension.isCustom" class="flex flex-row gap-2">
                        <UButton
                            label="Edit"
                            icon="i-lucide-pencil"
                            color="neutral"
                            variant="subtle"
                            size="sm"
                            :to="`/settings/sources/${extension.downloadExtensionsId}`" />
                        <UButton
                            label="Delete"
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

const toast = useToast();
const confirmModal = useOverlay().create(TrangaConfirmModal);

const { downloadExtensions } = await useDownloadExtensions();

const confirmDelete = (extension: ServicesMangaDownloadExtension) =>
    confirmModal.open({
        title: `Delete ${extension.name}`,
        description:
            'Manga that use this source keep their download links and chapters, but nothing can be searched, listed or downloaded through it anymore.',
        onConfirm: async () => {
            try {
                await useNuxtApp().$tranga(`/mangas/customDownloaders/${extension.downloadExtensionsId}`, { method: 'delete' });
            } catch (error: unknown) {
                toast.add({
                    title: 'Failed deleting the source.',
                    description: error instanceof FetchError ? (error.data ?? error.message) : undefined,
                    color: 'error',
                });
                return;
            }
            clearNuxtData(ApiKeys.CustomDownloader(extension.downloadExtensionsId));
            await refreshNuxtData([ApiKeys.DownloadExtensions, ApiKeys.CustomDownloaders]);
            toast.add({ title: `Deleted ${extension.name}.`, color: 'success' });
        },
    });
</script>
