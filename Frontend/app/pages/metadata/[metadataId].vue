<template>
    <MetadataPage :metadata="metadata" :actions="actions" :loading="statusMetadata !== 'success'"> </MetadataPage>
</template>

<script setup lang="ts">
import type { GetMetadataByMetadataIdResponse, MangaMetadata, PatchMangasByMangaIdUseMetadataResponses } from '~/api/trangaApi';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';

const metadataId = useRoute().params.metadataId as string;

const { $tranga } = useNuxtApp();
const toast = useToast();

const busy = ref<boolean>(false);

const { data: metadata, status: statusMetadata } = await useTranga<GetMetadataByMetadataIdResponse>(() => `/metadata/${metadataId}`, {
    key: ApiKeys.Metadata(metadataId),
});

const actions = (m?: MangaMetadata): ButtonProps[] | undefined => {
    const items: ButtonProps[] = [];

    if (metadata.value && metadata.value.mangaIds.length == 1) {
        items.push({
            label: 'Use as Source for Manga',
            onClick: async () => await patchSource(metadataId, metadata.value!.mangaIds[0]!),
            disabled: metadata.value!.chosen ?? false,
        });
    }

    return items;
};

const patchSource = async (metadataId: string, mangaId: string) => {
    try {
        busy.value = true;
        await $tranga<PatchMangasByMangaIdUseMetadataResponses>(`/mangas/${mangaId}/useMetadata`, {
            method: 'patch',
            body: { metadataId: metadataId },
        });
        await refreshNuxtData(ApiKeys.Metadata(metadataId));
    } catch {
        toast.add({ title: 'Could not set Metadata as Source!', color: 'error' });
    } finally {
        {
            busy.value = false;
        }
    }
};
</script>
