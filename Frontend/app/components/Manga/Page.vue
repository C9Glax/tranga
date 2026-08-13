<template>
    <TrangaPage :navigation="navigation">
        <div class="flex flex-col-reverse xl:flex-row gap-6 py-6 sm:py-8">
            <div class="flex-1 min-w-0 md:pe-6">
                <slot name="default" />
            </div>
            <div class="flex-1 w-full xl:max-w-130 flex flex-col gap-4 items-center xl:items-start">
                <MangaCover
                    :file-id="manga?.metadataEntry?.coverId"
                    :manga-id="manga?.mangaId"
                    :no-blur="!manga?.metadataEntry?.nsfw"
                    :size="COVER_SIZES.hero"
                    class="aspect-6/9 w-full max-w-2xs mx-auto md:mx-0" />

                <div class="flex flex-col gap-2">
                    <div class="flex flex-row gap-2 items-baseline flex-wrap">
                        <TrangaTitle v-if="displayTitle" :title="displayTitle" class="text-lg font-semibold" />
                        <USkeleton v-else class="h-lh w-14" />
                        <p v-if="manga?.metadataEntry?.year" class="text-dimmed text-sm">
                            {{ manga.metadataEntry?.year }}
                        </p>
                    </div>
                    <div class="flex flex-row gap-2 items-center flex-wrap">
                        <UBadge
                            v-if="manga?.metadataEntry?.status"
                            :label.camel="manga.metadataEntry.status"
                            :color="releaseStatusBadgeColor(manga.metadataEntry.status)"
                            variant="outline" />
                        <USkeleton v-else class="h-lh w-14" />
                        <UBadge v-if="manga?.metadataEntry?.nsfw" label="NSFW" color="error" variant="solid" />
                        <UBadge
                            v-if="manga"
                            :label="manga.monitored ? 'Monitored' : 'Not Monitored'"
                            :color="manga.monitored ? 'success' : 'neutral'"
                            :variant="manga.monitored ? 'solid' : 'outline'"
                            class="cursor-pointer select-none"
                            :class="{ 'opacity-50 pointer-events-none': togglingMonitored }"
                            @click="toggleMonitored" />
                    </div>
                    <UUser
                        v-if="manga?.metadataEntry"
                        :avatar="{
                            src: metadataExtensions.getExtension(manga.metadataEntry.metadataExtensionId)?.iconUrl ?? '/blahaj.png',
                        }"
                        :name="
                            metadataExtensions.getExtension(manga.metadataEntry.metadataExtensionId)?.name ??
                            manga.metadataEntry.metadataExtensionId
                        "
                        :description="manga.metadataEntry.identifier"
                        :to="manga.metadataEntry.url ?? undefined"
                        target="_blank" />
                    <USkeleton v-else class="h-lh w-32" />
                </div>

                <p v-if="$props.description">{{ $props.description }}</p>
                <UEditor
                    v-else-if="manga?.metadataEntry"
                    :model-value="manga.metadataEntry.summary"
                    content-type="markdown"
                    :editable="false"
                    :ui="{ base: 'sm:px-0 p-0 px-0 ps-0' }" />
                <div v-else class="flex flex-col gap-1">
                    <USkeleton class="h-lh mr-6" />
                    <USkeleton class="h-lh" />
                    <USkeleton class="h-lh mr-12" />
                </div>
            </div>
        </div>
    </TrangaPage>
</template>

<script setup lang="ts">
import { MangaCover } from '#components';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';
import type { GetLibrariesMappingsByMangaIdResponse, ServicesMangaManga } from '~/api/tranga';
import { releaseStatusBadgeColor } from '~/utils/releaseStatusBadgeColor';
import type { NavigationMenuItem } from '@nuxt/ui/components/NavigationMenu.vue';
import { ApiKeys } from '~/composables/ApiKeys';
import type { TrangaPageTitleProps } from '~/components/Tranga/Page.vue';
import useMetadataExtensions from '~/composables/MetadataExtension';

export interface MangaPageProps {
    title?: string;
    description?: string;
    manga?: ServicesMangaManga;
    actions?: (manga?: ServicesMangaManga) => ButtonProps[] | undefined;
}

const props = defineProps<MangaPageProps>();

const displayTitle = computed(() => props.title ?? props.manga?.metadataEntry?.series);

const togglingMonitored = ref(false);
const toggleMonitored = async () => {
    if (!props.manga?.mangaId || togglingMonitored.value) return;
    togglingMonitored.value = true;
    try {
        await patchMangaMonitored(props.manga.mangaId, !props.manga.monitored);
    } finally {
        togglingMonitored.value = false;
    }
};

const { metadataExtensions } = await useMetadataExtensions();

const { data: libraryMappings } = useTranga<GetLibrariesMappingsByMangaIdResponse>(() => `/libraries/mappings/${props.manga?.mangaId}`, {
    key: ApiKeys.Libraries.Mapping(props.manga?.mangaId ?? ''),
});

const navigation = computed((): TrangaPageTitleProps => {
    const actionItems: NavigationMenuItem[] = (props.actions?.(props.manga) ?? []).map(
        (action): NavigationMenuItem => ({
            label: action.label,
            icon: action.icon,
            to: action.to,
            target: action.target,
            onSelect: action.onClick
                ? (e: Event) => {
                      const handlers = Array.isArray(action.onClick) ? action.onClick : [action.onClick!];
                      handlers.forEach((handler) => void handler(e as unknown as MouseEvent));
                  }
                : undefined,
        }),
    );

    const komgaLinkItems: NavigationMenuItem[] = (libraryMappings.value ?? []).map(
        (mapping): NavigationMenuItem => ({
            label: 'View in Komga',
            to: mapping.seriesUrl,
            icon: 'i-lucide-external-link',
            target: '_blank',
        }),
    );

    return {
        title: { label: 'Manga', type: 'label' },
        items: [
            { label: 'Manga', to: `/manga/${props.manga?.mangaId}`, icon: 'i-lucide-book' },
            { label: 'Metadata-Entries', to: `/manga/${props.manga?.mangaId}/metadataEntries`, icon: 'i-lucide-list' },
            { label: 'Manga Tasks', to: `/tasks?manga=${props.manga?.mangaId}`, icon: 'i-lucide-biceps-flexed' },
            { label: 'Manga Downloads', to: `/manga/${props.manga?.mangaId}/downloads`, icon: 'i-lucide-cloud-download' },
            { label: 'Chapters', to: `/manga/${props.manga?.mangaId}/chapters`, icon: 'i-lucide-list-checks' },
            ...komgaLinkItems,
            ...(actionItems.length ? [{ label: 'Actions', type: 'label' } as NavigationMenuItem, ...actionItems] : []),
        ],
    };
});
</script>
