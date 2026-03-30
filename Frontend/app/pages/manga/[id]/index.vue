<template>
    <UPage>
        <UPageHero :title="data?.title" :links="links" orientation="horizontal" reverse>
            <MangaCover :mangaId="mangaId" noBlur />
        </UPageHero>
        <UBlogPosts>
            <MetadataExtensionOverview v-for="link in data?.metadataLinks" :key="link.metadataLinkId" :metadata-link="link" />
        </UBlogPosts>
    </UPage>
</template>

<script setup lang="ts">
import { MangaCover, MetadataExtensionOverview } from '#components';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';

const mangaId = useRoute().params.id as string;

const { data } = await api.GET('/manga/{mangaId}', {
    params: { path: { mangaId: mangaId }, query: { includes: ['DownloadLinks', 'MetadataLinks'] } },
});

const apiBaseUrl = useAppConfig().api.baseUrl;

const links: ButtonProps[] = [{ label: 'Match', to: `/manga/${mangaId}/match` }];
</script>
