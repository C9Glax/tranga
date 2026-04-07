<template>
    <TrangaList :loading="loading">
        <DownloadLinkListCard v-for="downloadLink in downloadLinks" :key="downloadLink.downloadId" :download-link="downloadLink" />
    </TrangaList>
</template>

<script setup lang="ts">
import type { DownloadLink } from '~/api/trangaApi';

defineProps<{ loading?: boolean; downloadLinks?: DownloadLink[] }>();
</script>
