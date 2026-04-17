<template>
    <TrangaChip icon="i-lucide-info" color="info" size="sm">
        <UBlogPost
            :title="metadata.series"
            :description="metadata.summary ?? undefined"
            :image="{ src: `http://${useRuntimeConfig().public.api.baseUrl}/mangas/files/${metadata.coverId}`, loading: 'lazy' }"
            :to="mangaId ? `/metadata/${metadata.metadataId}?mangaId=${mangaId}` : `/metadata/${metadata.metadataId}`"
            :target="target"
            external
            class="w-full h-full"
            :ui="{
                header: 'aspect-[13/9]',
                image: `object-center ${metadata.nsfw && 'blur-md'}`,
                description: 'h-30 text-ellipsis overflow-hidden',
            }">
            <template #badge>
                <UBadge v-if="metadata.status" :label.camel="metadata.status" :color="badgeColor(metadata.status)" />
                <UBadge v-if="metadata.nsfw" label="NSFW" color="error" variant="solid" />
            </template>
            <template #date>
                <p class="text-dimmed">{{ props.metadata.year }}</p>
            </template>
            <template #authors>
                <UUser
                    :avatar="{
                        src:
                            metadataExtensions?.find((e) => e.metadataExtensionId == metadata.metadataExtensionId)?.iconUrl ??
                            '/blahaj.png',
                    }"
                    :name="
                        metadataExtensions?.find((e) => e.metadataExtensionId == metadata.metadataExtensionId)?.name ??
                        metadata.metadataExtensionId
                    "
                    :description="metadata.identifier"
                    :to="metadata.url ?? undefined"
                    target="_blank"
                    :ui="{ description: 'truncate h-lh max-w-24' }" />
                <UButton v-for="prop in actions" v-bind="prop" />
            </template>
        </UBlogPost>
    </TrangaChip>
</template>

<script setup lang="ts">
import useMetadataExtensions from '~/composables/MetadataExtension';
import type { ServicesMangaMetadata, ServicesMangaReleaseStatus } from '~/api/tranga';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';

const props = defineProps<{
    metadata: ServicesMangaMetadata;
    actions?: ButtonProps[];
    mangaId?: string;
    target?: '_blank' | '_parent' | '_self' | '_top';
}>();

const { metadataExtensions } = await useMetadataExtensions();

const badgeColor = (status: ServicesMangaReleaseStatus) => {
    switch (status) {
        case 'Ongoing':
            return 'primary';
        case 'Hiatus':
            return 'warning';
        case 'Complete':
            return 'secondary';
        case 'Cancelled':
            return 'error';
        default:
            return 'neutral';
    }
};
</script>
