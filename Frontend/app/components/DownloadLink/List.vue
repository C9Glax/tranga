<template>
    <TrangaList :loading="loading" :empty="!downloadLinks || downloadLinks.length < 1">
        <DownloadLinkListCard v-for="downloadLink in downloadLinks" :key="downloadLink.downloadId" :download-link="downloadLink" />

        <template #empty>
            <UEmpty icon="i-lucide-download" :title="emptyTitle" :description="emptyDescription" :actions="emptyActions" class="mt-4" />
        </template>
    </TrangaList>
</template>

<script setup lang="ts">
import type { ServicesMangaMangaDownloadLink } from '~/api/tranga';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';

defineProps<{
    loading?: boolean;
    downloadLinks?: ServicesMangaMangaDownloadLink[];
    emptyTitle?: string;
    emptyDescription?: string;
    emptyActions?: ButtonProps[];
}>();
</script>
