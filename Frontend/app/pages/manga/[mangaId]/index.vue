<template>
    <MangaPage :manga="manga" :actions="actions" :loading="statusManga !== 'success'">
        <UBlogPosts>
            <USkeleton v-if="statusMetadata !== 'success'" class="w-full h-96" />
            <UChip v-else v-for="metadata in metadataSources" :key="metadata.metadataId" :ui="{ base: 'h-7 w-7' }" color="info">
                <template #content>
                    <UIcon name="i-lucide-info" class="size-5" />
                </template>
                <UBlogPost
                    :description="metadata.summary ?? undefined"
                    :image="{ src: `http://${useRuntimeConfig().public.api.baseUrl}/files/${metadata.coverId}`, loading: 'lazy' }"
                    :to="`/metadata/${metadata.metadataId}`"
                    external
                    :authors="author(metadata)"
                    :badge="badge(metadata)"
                    :date="date(metadata)"
                    :ui="{ header: 'aspect-[13/9]', image: 'object-center', description: 'h-30 text-ellipsis overflow-hidden' }">
                    <template #authors>
                        <UButton
                            :disabled="metadata.chosen ?? false"
                            :variant="metadata.chosen ? 'outline' : 'soft'"
                            :label="metadata.chosen ? 'Is Source' : 'Use as Source'"
                            @click="async () => await patchMangaMetadataSource(metadata.metadataId, mangaId)" />
                    </template>
                </UBlogPost>
            </UChip>
        </UBlogPosts>
    </MangaPage>
</template>

<script setup lang="ts">
import type { GetMangasByMangaIdMetadataResponse, GetMangasByMangaIdResponse, Manga, Metadata } from '~/api/trangaApi';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';
import type { UserProps } from '@nuxt/ui/components/User.vue';
import type { BadgeProps } from '@nuxt/ui/components/Badge.vue';
import { patchMangaMetadataSource } from '~/utils/patchMangaMetadataSource';

const mangaId = useRoute().params.mangaId as string;

const { data: manga, status: statusManga } = await useTranga<GetMangasByMangaIdResponse>(() => `/mangas/${mangaId}`, {
    key: ApiKeys.Manga(mangaId),
});

const { data: metadataSources, status: statusMetadata } = await useTranga<GetMangasByMangaIdMetadataResponse>(
    () => `/mangas/${mangaId}/metadata`,
    { key: ApiKeys.MangaMetadata(mangaId), lazy: true }
);

const actions = (manga?: Manga): ButtonProps[] | undefined => [];

const author = (metadata: Metadata): UserProps[] => [
    {
        avatar: { src: MetadataExtensions.GetIcon(metadata.metadataExtensionId) },
        name: MetadataExtensions.GetName(metadata.metadataExtensionId),
    },
];

const badge = (metadata: Metadata): BadgeProps | undefined => {
    if (!metadata.status) return undefined;
    let color: 'error' | 'primary' | 'secondary' | 'success' | 'info' | 'warning' | 'neutral' | undefined = 'neutral';
    switch (metadata.status) {
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
    return { label: metadata.status, color: color };
};

const date = (metadata: Metadata) => {
    if (!metadata.year) return undefined;
    return new Date(`${metadata.year}`);
};
</script>
