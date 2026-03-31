<template>
    <UChip :ui="{ base: 'h-7 w-7', root: 'h-128' }">
        <template #content>
            <UIcon name="i-lucide-download" class="size-5" />
        </template>
        <UBlogPost
            :description="downloadLink.description ?? undefined"
            :image="{ src: `${apiBaseUrl}/file/${downloadLink.coverFileId}`, loading: 'lazy' }"
            :to="`/manga/${mangaId}/match/${downloadLink.downloadLinkId}`"
            external
            :badge="{ label: downloadLink.downloadLinkId }"
            :authors="author"
            :ui="{ header: 'aspect-[13/9]', image: 'object-center', description: 'h-30 text-ellipsis overflow-hidden' }" />
    </UChip>
</template>

<script setup lang="ts">
import type { UserProps } from '@nuxt/ui/components/User.vue';
import type { DownloadLinkDto } from '~/api/trangaApi';

const apiBaseUrl = useAppConfig().api.baseUrl;
const mangaId = useRoute().params.mangaId as string;

const props = defineProps<{ downloadLink: DownloadLinkDto }>();

const author = computed((): UserProps[] => [
    {
        avatar: { src: DownloadExtensions.GetIcon(props.downloadLink.downloadExtensionId) },
        name: DownloadExtensions.GetName(props.downloadLink.downloadExtensionId),
    },
]);
</script>
