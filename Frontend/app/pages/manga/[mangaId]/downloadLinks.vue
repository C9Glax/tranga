<template>
    <TrangaPage :navigation="navigation">
        <UPageSection :ui="{ container: 'sm:py-0 lg:py-0 gap-8 sm:gap-8' }" :title="$t('downloadLinks.searchResult')">
            <div class="flex flex-row gap-2">
                <UInput
                    v-model="searchTerm"
                    :placeholder="$t('downloadLinks.searchPlaceholder')"
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
                    :placeholder="$t('downloadLinks.extension')"
                    class="w-48" />
                <span ref="add-link">
                    <UButton
                        :label="$t('downloadLinks.addLink')"
                        icon="i-lucide-plus"
                        loading-auto
                        :disabled="!addExtensionId || !searchTerm"
                        @click="addLink" />
                </span>
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
const { t } = useI18n();
const toast = useToast();

useTourTargetRef('add-link');

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
        const description = error instanceof FetchError ? (error.data ?? error.message) : t('downloadLinks.addFailedFallback');
        toast.add({ title: t('downloadLinks.addFailed'), description, color: 'error' });
        return;
    }

    await refreshDownloadLinks();
    toast.add({ title: t('downloadLinks.added'), color: 'success' });
    searchTerm.value = '';
    addExtensionId.value = undefined;
};

const navigation = computed((): TrangaPageTitleProps => {
    return {
        title: { label: t('manga.nav.manga'), type: 'label' },
        items: [
            { label: t('manga.nav.manga'), to: `/manga/${mangaId}`, icon: 'i-lucide-book' },
            { label: t('manga.nav.metadataEntries'), to: `/manga/${mangaId}/metadataEntries`, icon: 'i-lucide-list' },
            { label: t('manga.nav.tasks'), to: `/tasks?manga=${mangaId}`, icon: 'i-lucide-biceps-flexed' },
            { label: t('manga.nav.downloadTasks'), to: `/manga/${mangaId}/downloads`, icon: 'i-lucide-cloud-download' },
            { label: t('common.chapters'), to: `/manga/${mangaId}/chapters`, icon: 'i-lucide-list-checks' },
        ],
    };
});
</script>
