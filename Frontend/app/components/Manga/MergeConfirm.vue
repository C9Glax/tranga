<template>
    <UModal class="py-8" title="Merge Manga" :ui="{ content: 'max-w-2xl' }">
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

                    <div class="flex gap-3 rounded-md border border-default p-3 mt-1">
                        <MangaCover :file-id="previewMetadata?.coverId" no-blur class="w-16 shrink-0 rounded" />
                        <div class="flex flex-col gap-1 min-w-0">
                            <p class="text-xs text-dimmed uppercase tracking-wide">Preview</p>
                            <p class="font-semibold truncate">{{ previewMetadata?.series ?? 'No metadata' }}</p>
                            <p class="text-sm text-dimmed line-clamp-3">{{ previewMetadata?.summary }}</p>
                        </div>
                    </div>
                </div>

                <div class="flex flex-col gap-2">
                    <p class="font-semibold">Chapters</p>
                    <URadioGroup
                        v-model="keepSourceChapters"
                        orientation="horizontal"
                        :items="[
                            {
                                label: `Keep ${targetManga.metadataEntry?.series ?? 'Target'}'s Chapters (${targetChapters?.length ?? '...'})`,
                                value: false,
                            },
                            {
                                label: `Keep ${sourceManga?.metadataEntry?.series ?? 'Source'}'s Chapters (${sourceChapters?.length ?? '...'})`,
                                value: true,
                            },
                        ]" />

                    <p class="text-sm text-dimmed">
                        Merged Manga will have
                        <span class="font-semibold">{{ (keepSourceChapters ? sourceChapters : targetChapters)?.length ?? '...' }}</span>
                        Chapters. The other Manga's Chapters are discarded.
                    </p>
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
import { MangaCover } from '#components';
import type { GetMangasByMangaIdChaptersResponse, GetMangasByMangaIdResponse, ServicesMangaManga } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';

const props = defineProps<{ sourceMangaId: string; targetManga: ServicesMangaManga }>();
const emit = defineEmits(['close']);

const { data: sourceManga } = useTranga<GetMangasByMangaIdResponse>(() => `/mangas/${props.sourceMangaId}`, {
    key: ApiKeys.Manga.Manga(props.sourceMangaId),
});

const { data: targetChapters } = useTranga<GetMangasByMangaIdChaptersResponse>(() => `/mangas/${props.targetManga.mangaId}/chapters`, {
    key: ApiKeys.Manga.Chapters.List(props.targetManga.mangaId),
});
const { data: sourceChapters } = useTranga<GetMangasByMangaIdChaptersResponse>(() => `/mangas/${props.sourceMangaId}/chapters`, {
    key: ApiKeys.Manga.Chapters.List(props.sourceMangaId),
});

const keepSourceMetadata = ref(false);
const keepSourceChapters = ref(false);
const merging = ref(false);

const previewMetadata = computed(() => (keepSourceMetadata.value ? sourceManga.value?.metadataEntry : props.targetManga.metadataEntry));

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
