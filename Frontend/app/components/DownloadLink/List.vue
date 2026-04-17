<template>
    <TrangaList :loading="loading">
        <DownloadLinkListCard v-for="downloadLink in downloadLinks" :key="downloadLink.downloadId" :download-link="downloadLink" />
    </TrangaList>
</template>

<script setup lang="ts">
import type { ServicesMangaMangaDownloadLink } from '~/api/tranga';

defineProps<{ loading?: boolean; downloadLinks?: ServicesMangaMangaDownloadLink[] }>();
</script>
