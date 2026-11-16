<template>
    <UModal class="py-8" :title="$t('manga.merge.title')" :ui="{ content: 'max-w-2xl' }">
        <template #content>
            <div class="flex flex-col gap-6 p-6">
                <i18n-t keypath="manga.merge.intro" tag="p" class="text-sm text-dimmed">
                    <template #target>
                        <TrangaTitle :title="targetManga.metadataEntry?.series" class="font-semibold" />
                    </template>
                    <template #source>
                        <TrangaTitle :title="sourceManga?.metadataEntry?.series ?? '...'" class="font-semibold" />
                    </template>
                </i18n-t>

                <div class="flex flex-col gap-2">
                    <p class="font-semibold">{{ $t('manga.merge.titleSummaryCover') }}</p>
                    <URadioGroup v-model="keepSourceMetadata" orientation="horizontal" :items="metadataItems" />

                    <div class="flex gap-3 rounded-md border border-default p-3 mt-1">
                        <div class="flex-1 min-w-0">
                            <MangaCover :file-id="previewMetadata?.coverId" :size="COVER_SIZES.card" no-blur class="rounded" />
                        </div>
                        <div class="flex-1 min-w-0 flex flex-col gap-1">
                            <p class="text-xs text-dimmed uppercase tracking-wide">{{ $t('manga.merge.preview') }}</p>
                            <TrangaTitle :title="previewMetadata?.series ?? $t('manga.merge.noMetadata')" class="font-semibold" />
                            <p class="text-sm text-dimmed line-clamp-3">{{ previewMetadata?.summary }}</p>
                        </div>
                    </div>
                </div>

                <div class="flex flex-col gap-2">
                    <p class="font-semibold">{{ $t('common.chapters') }}</p>
                    <URadioGroup v-model="keepSourceChapters" orientation="horizontal" :items="chapterItems" />

                    <i18n-t keypath="manga.merge.resultingChapters" tag="p" class="text-sm text-dimmed">
                        <template #count>
                            <span class="font-semibold">{{ (keepSourceChapters ? sourceChapters : targetChapters)?.length ?? '...' }}</span>
                        </template>
                    </i18n-t>
                </div>

                <div class="flex justify-end gap-2">
                    <UButton :label="$t('common.cancel')" variant="ghost" @click="emit('close')" />
                    <UButton :label="$t('manga.merge.merge')" color="error" :loading="merging" @click="confirm" />
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

const { t } = useI18n();

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

const metadataItems = computed(() => [
    { label: t('manga.merge.keepTitle', { title: props.targetManga.metadataEntry?.series ?? t('manga.merge.target') }), value: false },
    { label: t('manga.merge.useTitle', { title: sourceManga.value?.metadataEntry?.series ?? t('manga.merge.source') }), value: true },
]);

const chapterItems = computed(() => [
    {
        label: t('manga.merge.keepChaptersOf', {
            title: props.targetManga.metadataEntry?.series ?? t('manga.merge.target'),
            count: targetChapters.value?.length ?? '...',
        }),
        value: false,
    },
    {
        label: t('manga.merge.keepChaptersOf', {
            title: sourceManga.value?.metadataEntry?.series ?? t('manga.merge.source'),
            count: sourceChapters.value?.length ?? '...',
        }),
        value: true,
    },
]);

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
