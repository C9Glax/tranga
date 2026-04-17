<template>
    <TrangaList :loading="loading">
        <MetadataListCard
            v-for="metadata in metadataList"
            :key="metadata.metadataExtensionId"
            :metadata="metadata"
            :actions="actions ? actions(metadata) : undefined"
            :manga-id="mangaId"
            :target="target" />
    </TrangaList>
</template>

<script setup lang="ts">
import type { ServicesMangaMetadata } from '~/api/tranga';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';

defineProps<{
    loading?: boolean;
    metadataList?: ServicesMangaMetadata[];
    actions?: (metadata: ServicesMangaMetadata) => ButtonProps[];
    mangaId?: string;
    target?: '_blank' | '_parent' | '_self' | '_top';
}>();
</script>
