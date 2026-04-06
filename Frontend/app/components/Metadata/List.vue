<template>
    <UBlogPosts>
        <USkeleton v-if="loading" class="w-full h-96" />
        <MetadataListCard
            v-for="metadata in metadataList"
            :key="metadata.metadataExtensionId"
            :metadata="metadata"
            :actions="actions ? actions(metadata) : undefined" />
    </UBlogPosts>
</template>

<script setup lang="ts">
import type { Metadata } from '~/api/trangaApi';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';

defineProps<{ loading?: boolean; metadataList?: Metadata[]; actions?: (metadata: Metadata) => ButtonProps[] }>();
</script>
