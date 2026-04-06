<template>
    <TrangaChip icon="i-lucide-info" color="info" size="sm">
        <UBlogPost
            :title="metadata.series"
            :description="metadata.summary ?? undefined"
            :image="{ src: `http://${useRuntimeConfig().public.api.baseUrl}/files/${metadata.coverId}`, loading: 'lazy' }"
            :to="`/metadata/${metadata.metadataId}`"
            external
            :badge="badge"
            :date="date"
            class="w-full h-full"
            :ui="{ header: 'aspect-[13/9]', image: 'object-center', description: 'h-30 text-ellipsis overflow-hidden' }">
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
                    " />
                <UButton v-for="prop in actions" v-bind="prop" />
            </template>
        </UBlogPost>
    </TrangaChip>
</template>

<script setup lang="ts">
import useMetadataExtensions from '~/composables/MetadataExtension';
import type { Metadata } from '~/api/trangaApi';
import type { BadgeProps } from '@nuxt/ui/components/Badge.vue';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';

const props = defineProps<{ metadata: Metadata; actions?: ButtonProps[] }>();

const { metadataExtensions } = await useMetadataExtensions();

const badge = computed((): BadgeProps | undefined => {
    if (!props.metadata.status) return undefined;
    let color: 'error' | 'primary' | 'secondary' | 'success' | 'info' | 'warning' | 'neutral' | undefined = 'neutral';
    switch (props.metadata.status) {
        case 'Ongoing':
            color = 'primary';
            break;
        case 'Hiatus':
            color = 'warning';
            break;
        case 'Complete':
            color = 'secondary';
            break;
        case 'Cancelled':
            color = 'error';
            break;
    }
    return { label: props.metadata.status, color: color };
});

const date = computed(() => {
    if (!props.metadata.year) return undefined;
    return new Date(`${props.metadata.year}`);
});
</script>
