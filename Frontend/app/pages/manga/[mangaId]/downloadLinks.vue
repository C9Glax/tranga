<template>
    <TrangaPage :navigation="navigation">
        <UPageSection :ui="{ container: 'sm:py-0 lg:py-0 gap-8 sm:gap-8' }" title="Search Result">
            <div class="flex flex-row gap-2">
                <UInput
                    v-model="searchTerm"
                    placeholder="Search term, or paste a manga URL to add it directly"
                    :loading="statusDownloadLinks === 'pending'"
                    :disabled="statusDownloadLinks === 'pending'"
                    class="w-full"
                    @keyup.enter="refreshDownloadLinks">
                    <template #trailing>
                        <UIcon class="cursor-pointer" name="i-lucide-search" @click="refreshDownloadLinks" />
                    </template>
                </UInput>
                <USelectMenu
                    v-model="addExtensionId"
                    :items="downloadExtensionOptions"
                    :loading="!downloadExtensions"
                    value-key="value"
                    searchable
                    placeholder="Extension"
                    class="w-48" />
                <UButton label="Add Link" icon="i-lucide-plus" loading-auto :disabled="!addExtensionId || !searchTerm" @click="addLink" />
            </div>
            <DownloadLinkList :download-links="downloadLinks" :loading="statusDownloadLinks !== 'success'" />
        </UPageSection>
    </TrangaPage>
</template>

<script setup lang="ts">
import type { PostMangasSearchByMangaIdDownloadLinksResponse } from '~/api/tranga';
import type { NavigationMenuProps } from '@nuxt/ui/components/NavigationMenu.vue';
import useDownloadExtensions from '~/composables/DownloadExtension';
import { FetchError } from 'ofetch';
import type { TrangaPageTitleProps } from '~/components/Tranga/Page.vue';

const mangaId = useRoute().params.mangaId as string;
const toast = useToast();

const searchTerm = ref<string>('');

const {
    data: downloadLinks,
    status: statusDownloadLinks,
    refresh: refreshDownloadLinks,
} = useTranga<PostMangasSearchByMangaIdDownloadLinksResponse>(() => `/mangas/search/${mangaId}/downloadLinks`, {
    method: 'POST',
    body: computed(() => (searchTerm.value ? { searchTerm: searchTerm.value } : undefined)),
    watch: false,
});

const { downloadExtensions } = await useDownloadExtensions();

const downloadExtensionOptions = computed(
    () => downloadExtensions.value?.map((e) => ({ label: e.name ?? e.downloadExtensionsId, value: e.downloadExtensionsId })) ?? [],
);

const addExtensionId = ref<string | undefined>();

const addLink = async () => {
    if (!addExtensionId.value || !searchTerm.value) return;
    try {
        await useNuxtApp().$tranga(`/mangas/${mangaId}/downloadLinks`, {
            method: 'post',
            body: { downloadExtensionId: addExtensionId.value, url: searchTerm.value },
        });
    } catch (error: unknown) {
        const description = error instanceof FetchError ? (error.data ?? error.message) : 'Could not add the download-link.';
        toast.add({ title: 'Failed adding download-link.', description, color: 'error' });
        return;
    }

    await refreshDownloadLinks();
    toast.add({ title: 'Added download-link.', color: 'success' });
    searchTerm.value = '';
    addExtensionId.value = undefined;
};

const navigation = computed((): TrangaPageTitleProps => {
    return {
        title: { label: 'Manga', type: 'label' },
        items: [
            { label: 'Manga', to: `/manga/${mangaId}`, icon: 'i-lucide-book' },
            { label: 'Metadata-Entries', to: `/manga/${mangaId}/metadataEntries`, icon: 'i-lucide-list' },
            { label: 'Manga Tasks', to: `/tasks?manga=${mangaId}`, icon: 'i-lucide-biceps-flexed' },
            { label: 'Manga Download Tasks', to: `/manga/${mangaId}/downloads`, icon: 'i-lucide-cloud-download' },
            { label: 'Chapters', to: `/manga/${mangaId}/chapters`, icon: 'i-lucide-list-checks' },
        ],
    };
});
</script>
