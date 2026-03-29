<template>
    <UPage>
        <MangaCover :mangaId="mangaId" no-blur />
        {{ data?.title }}
        {{ data?.monitored }}
        {{ data?.downloadLinks }}
        <MetadataExtensionOverview v-for="link in data?.metadataLinks" :metadata-link="link" />
    </UPage>
</template>

<script setup lang="ts">
import { MangaCover, MetadataExtensionOverview } from '#components';

const mangaId = useRoute().params.id as string;

const { data } = await api.GET('/manga/{mangaId}', {
    params: { path: { mangaId: mangaId }, query: { includes: ['DownloadLinks', 'MetadataLinks'] } },
});
</script>
