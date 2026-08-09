<template>
    <TrangaPage :page-title="{ title: 'Manga', icon: { name: 'i-lucide-book', color: 'warning' } }" :navigation-props="navigation">
        <div class="flex flex-col-reverse lg:flex-row gap-8 lg:gap-12 py-6 sm:py-8">
            <div class="flex-1 min-w-0">
                <slot name="default" />
            </div>

            <aside class="lg:w-72 xl:w-80 lg:shrink-0 flex flex-col gap-4 lg:sticky lg:top-4 lg:self-start">
                <MangaCover
                    :file-id="manga?.metadataEntry?.coverId"
                    :mangaId="manga?.mangaId"
                    :noBlur="!manga?.metadataEntry?.nsfw"
                    class="aspect-6/9 w-full max-w-2xs mx-auto lg:mx-0" />

                <div class="flex flex-col gap-2">
                    <div class="flex flex-row gap-2 items-baseline flex-wrap">
                        <p v-if="$props.title" class="text-lg font-semibold">{{ $props.title }}</p>
                        <p v-else-if="manga?.metadataEntry?.series" class="text-lg font-semibold">{{ manga?.metadataEntry?.series }}</p>
                        <USkeleton v-else class="h-lh w-14" />
                        <p v-if="manga?.metadataEntry?.year" class="text-dimmed text-sm">{{ manga.metadataEntry?.year }}</p>
                    </div>
                    <div class="flex flex-row gap-2 items-center flex-wrap">
                        <UBadge
                            v-if="manga?.metadataEntry?.status"
                            :label.camel="manga.metadataEntry.status"
                            :color="releaseStatusBadgeColor(manga.metadataEntry.status)"
                            variant="outline" />
                        <USkeleton v-else class="h-lh w-14" />
                        <UBadge v-if="manga?.metadataEntry?.nsfw" label="NSFW" color="error" variant="solid" />
                    </div>
                </div>

                <p v-if="$props.description">{{ $props.description }}</p>
                <UEditor
                    v-else-if="manga?.metadataEntry"
                    v-model="manga.metadataEntry.summary"
                    content-type="markdown"
                    :editable="false"
                    :ui="{ base: 'sm:px-0 p-0 px-0 ps-0' }" />
                <div v-else class="flex flex-col gap-1">
                    <USkeleton class="h-lh mr-6" />
                    <USkeleton class="h-lh" />
                    <USkeleton class="h-lh mr-12" />
                </div>
            </aside>
        </div>
    </TrangaPage>
</template>

<script setup lang="ts">
import { MangaCover } from '#components';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';
import type { ServicesMangaManga } from '~/api/tranga';
import { releaseStatusBadgeColor } from '~/utils/releaseStatusBadgeColor';
import type { NavigationMenuItem, NavigationMenuProps } from '@nuxt/ui/components/NavigationMenu.vue';

export interface MangaPageProps {
    title?: string;
    description?: string;
    manga?: ServicesMangaManga;
    actions?: (manga?: ServicesMangaManga) => ButtonProps[] | undefined;
}

const props = defineProps<MangaPageProps>();

const navigation = computed((): NavigationMenuProps => {
    const actionItems: NavigationMenuItem[] = (props.actions?.(props.manga) ?? []).map(
        (action): NavigationMenuItem => ({ label: action.label, icon: action.icon, to: action.to, target: action.target })
    );

    return {
        items: [
            { label: 'Manga', type: 'label' },
            { label: 'Manga', to: `/manga/${props.manga?.mangaId}`, icon: 'i-lucide-book' },
            { label: 'Metadata-Entries', to: `/manga/${props.manga?.mangaId}/metadataEntries`, icon: 'i-lucide-list' },
            { label: 'Manga Tasks', to: `/tasks?manga=${props.manga?.mangaId}`, icon: 'i-lucide-biceps-flexed' },
            { label: 'Manga Downloads', to: `/manga/${props.manga?.mangaId}/downloads`, icon: 'i-lucide-cloud-download' },
            ...(actionItems.length ? [{ label: 'Actions', type: 'label' } as NavigationMenuItem, ...actionItems] : []),
        ],
    };
});
</script>
