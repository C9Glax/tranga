<template>
    <MangaPage :manga="manga" :cover-file-id="entry?.coverFileId ?? undefined" :actions="actions">
        <template #description>
            <UEditor v-model="v" content-type="markdown" :editable="false" :ui="{ base: 'sm:px-0 p-0 px-0 ps-0' }" />
        </template>
    </MangaPage>
</template>

<script setup lang="ts">
import type { GetMangaByMangaIdResponse, MangaDto } from '~/api/trangaApi';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';

const mangaId = useRoute().params.mangaId as string;
const metadataId = useRoute().params.metadataId as string;

const { data: manga } = await useTranga<GetMangaByMangaIdResponse>(() => `/manga/${mangaId}`, {
    key: ApiKeys.Manga(mangaId, ['MetadataLinks']),
    query: { includes: ['MetadataLinks'] },
});

const entry = computed(() => manga.value?.metadataLinks?.find((l) => l.metadataLinkId === metadataId));

const v = ref(entry.value?.description);

const actions = (manga?: MangaDto): ButtonProps[] | undefined => [
    { label: 'Linked', to: `/manga/${manga?.mangaId}`, icon: 'i-lucide-arrow-left', variant: 'outline' },
    {
        label: 'View',
        avatar: { src: MetadataExtensions.GetIcon(entry.value?.metadataExtensionId) },
        variant: 'soft',
        to: entry.value?.url ?? undefined,
        target: '_blank',
    },
];
</script>
