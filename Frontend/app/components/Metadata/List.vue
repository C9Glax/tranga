<template>
    <TrangaList :loading="loading">
        <MetadataListCard
            v-for="metadata in metadataList"
            :key="metadata.metadataExtensionId"
            :metadata="metadata"
            :actions="actions ? actions(metadata) : undefined"
            :manga-id="mangaId" />
    </TrangaList>
</template>

<script setup lang="ts">
import type { Metadata } from '~/api/trangaApi';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';

defineProps<{ loading?: boolean; metadataList?: Metadata[]; actions?: (metadata: Metadata) => ButtonProps[]; mangaId?: string }>();
</script>
