<template>
    <TrangaChip icon="i-lucide-download" color="primary" size="sm">
        <UBlogPost
            :title="downloadLink.series"
            :description="downloadLink.summary ?? undefined"
            :image="{ src: `http://${useRuntimeConfig().public.api.baseUrl}/mangas/files/${downloadLink.coverId}`, loading: 'lazy' }"
            external
            class="w-full h-full"
            :ui="{
                header: 'aspect-[13/9]',
                image: `object-center ${downloadLink.nsfw && 'blur-md'}`,
                description: 'h-30 text-ellipsis overflow-hidden',
            }">
            <template #badge>
                <UBadge v-if="downloadLink.language" :label="downloadLink.language" />
                <UBadge v-if="downloadLink.nsfw" label="NSFW" color="error" variant="solid" />
            </template>
            <template #authors>
                <div class="flex flex-row gap-4">
                    <UUser
                        :avatar="{
                            src:
                                downloadExtensions?.find((e) => e.downloadExtensionsId == downloadLink.downloadExtensionId)?.iconUrl ??
                                '/blahaj.png',
                        }"
                        :name="
                            downloadExtensions?.find((e) => e.downloadExtensionsId == downloadLink.downloadExtensionId)?.name ??
                            downloadLink.downloadExtensionId
                        "
                        :description="downloadLink.identifier"
                        :to="downloadLink.url ?? undefined"
                        target="_blank"
                        :ui="{ description: 'truncate h-lh max-w-24' }" />
                    <UFieldGroup v-if="mDl">
                        <UButton v-if="!mDl.matched" variant="soft" label="Match" @click="updateMatch(true)" />
                        <UButton v-if="mDl.matched" variant="outline" label="Unmatch" @click="updateMatch(false)" />
                        <UInputNumber :default-value="mDl.priority as number" @update:model-value="(v) => updateMatch(undefined, v)" />
                    </UFieldGroup>
                </div>
            </template>
        </UBlogPost>
    </TrangaChip>
</template>

<script setup lang="ts">
import useDownloadExtensions from '~/composables/DownloadExtension';
import type { ServicesMangaMangaDownloadLink, ServicesMangaPatchMangaDownloadLinkRequest } from '~/api/tranga';
import { patchMangaDownloadLink } from '~/utils/patchMangaDownloadLink';

const props = defineProps<{ downloadLink: ServicesMangaMangaDownloadLink }>();

const mDl = computed(() => props.downloadLink as ServicesMangaMangaDownloadLink | undefined);

const { downloadExtensions } = await useDownloadExtensions();

const updateMatch = async (matched?: boolean, priority?: number) => {
    if (!mDl.value) return;
    if (matched === undefined && priority === undefined) return;
    const data: ServicesMangaPatchMangaDownloadLinkRequest = {
        matched: matched ?? mDl.value.matched,
        priority: priority ?? mDl.value.priority,
    };
    await patchMangaDownloadLink(props.downloadLink.downloadId, mDl.value.mangaId, data);
    await navigateTo(`/manga/${mDl.value.mangaId}`);
};
</script>
