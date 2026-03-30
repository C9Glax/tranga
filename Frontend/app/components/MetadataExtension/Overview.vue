<template>
    <UBlogPost
        :description="metadataLink.description ?? undefined"
        :image="{ src: `${apiBaseUrl}/file/${metadataLink.coverFileId}`, loading: 'lazy' }"
        :to="metadataLink.url ?? undefined"
        target="_blank"
        external
        :authors="author"
        :badge="badge"
        :date="date"
        :ui="{ header: 'aspect-[13/9]', image: 'object-center' }" />
</template>

<script setup lang="ts">
import { MetadataExtensions } from '~/composables/metadataExtension';
import type { BadgeProps } from '@nuxt/ui/components/Badge.vue';
import type { UserProps } from '@nuxt/ui/components/User.vue';
import type { MetadataLinkDto } from '~/api/trangaApi';

const apiBaseUrl = useAppConfig().api.baseUrl;

const props = defineProps<{ metadataLink: MetadataLinkDto }>();

const author = computed((): UserProps[] => [
    {
        avatar: { src: MetadataExtensions.GetIcon(props.metadataLink.metadataExtensionId) },
        name: MetadataExtensions.GetName(props.metadataLink.metadataExtensionId),
    },
]);

const badge = computed((): BadgeProps => {
    let color: 'error' | 'primary' | 'secondary' | 'success' | 'info' | 'warning' | 'neutral' | undefined = 'neutral';
    switch (props.metadataLink.status) {
        case 'Releasing':
            color = 'primary';
            break;
        case 'Pending':
            color = 'info';
            break;
        case 'Hiatus':
            color = 'warning';
            break;
        case 'Finished':
            color = 'secondary';
            break;
        case 'Cancelled':
            color = 'error';
            break;
    }
    return { label: props.metadataLink.status, color: color };
});

const date = computed(() => {
    if (!props.metadataLink.year) return undefined;
    return new Date(`${props.metadataLink.year}`);
});
</script>
