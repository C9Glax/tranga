<template>
    <UModal class="py-8" title="Merge Manga">
        <template #content>
            <div class="flex flex-col gap-6 p-6">
                <p class="text-sm text-dimmed">
                    Merging combines both Manga into <span class="font-semibold">{{ targetManga.metadataEntry?.series }}</span
                    >. <span class="font-semibold">{{ sourceManga?.metadataEntry?.series ?? '...' }}</span> will be deleted. Download Links
                    from both Manga are always combined automatically.
                </p>

                <div class="flex flex-col gap-2">
                    <p class="font-semibold">Title, Summary &amp; Cover</p>
                    <URadioGroup
                        v-model="keepSourceMetadata"
                        orientation="horizontal"
                        :items="[
                            { label: `Keep ${targetManga.metadataEntry?.series ?? 'Target'}`, value: false },
                            { label: `Use ${sourceManga?.metadataEntry?.series ?? 'Source'}`, value: true },
                        ]" />
                </div>

                <div class="flex flex-col gap-2">
                    <p class="font-semibold">Chapters</p>
                    <URadioGroup
                        v-model="keepSourceChapters"
                        orientation="horizontal"
                        :items="[
                            { label: `Keep ${targetManga.metadataEntry?.series ?? 'Target'}'s Chapters`, value: false },
                            { label: `Keep ${sourceManga?.metadataEntry?.series ?? 'Source'}'s Chapters`, value: true },
                        ]" />
                </div>

                <div class="flex justify-end gap-2">
                    <UButton label="Cancel" variant="ghost" @click="emit('close')" />
                    <UButton label="Merge" color="error" :loading="merging" @click="confirm" />
                </div>
            </div>
        </template>
    </UModal>
</template>

<script setup lang="ts">
import type { GetMangasByMangaIdResponse, ServicesMangaManga } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';

const props = defineProps<{ sourceMangaId: string; targetManga: ServicesMangaManga }>();
const emit = defineEmits(['close']);

const { data: sourceManga } = useTranga<GetMangasByMangaIdResponse>(() => `/mangas/${props.sourceMangaId}`, {
    key: ApiKeys.Manga.Manga(props.sourceMangaId),
});

const keepSourceMetadata = ref(false);
const keepSourceChapters = ref(false);
const merging = ref(false);

const confirm = async () => {
    merging.value = true;
    try {
        await mergeManga(props.targetManga.mangaId, props.sourceMangaId, keepSourceMetadata.value, keepSourceChapters.value);
    } finally {
        merging.value = false;
        emit('close');
    }
};
</script>
