<template>
    <TrangaPage :page-title="{ title: 'Manga', icon: { name: 'i-lucide-book', color: 'warning' } }">
        <UPageCTA v-bind="$props" :links="links" orientation="horizontal" :ui="{ container: 'py-6 sm:py-8 lg:py-8' }" class="w-full h-max">
            <template #title>
                <UBadge v-if="manga?.metadataEntry?.nsfw" label="NSFW" color="error" variant="solid" />
                <p v-if="$props.title">{{ $props.title }}</p>
                <p v-else-if="manga?.metadataEntry?.series">{{ manga?.metadataEntry?.series }}</p>
                <p v-if="manga?.metadataEntry?.year" class="text-dimmed text-sm">{{ manga.metadataEntry?.year }}</p>
                <USkeleton v-else class="h-lh" />
            </template>

            <template #description>
                <p v-if="$props.title">{{ $props.title }}</p>
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
            </template>

            <!-- Passes through the slots -->
            <template v-for="(_, slotName) in $slots" #[slotName]="slotProps">
                <slot v-if="slotName !== 'default'" :name="slotName as unknown" v-bind="slotProps" />
                <MangaCover
                    v-else
                    :file-id="manga?.metadataEntry?.coverId"
                    :mangaId="manga?.mangaId"
                    :noBlur="!manga?.metadataEntry?.nsfw"
                    class="aspect-6/9 max-h-sm max-w-sm" />
            </template>
            <MangaCover
                :file-id="manga?.metadataEntry?.coverId"
                :mangaId="manga?.mangaId"
                :noBlur="!manga?.metadataEntry?.nsfw"
                class="aspect-6/9 max-h-sm max-w-sm" />
        </UPageCTA>

        <UPageSection :ui="{ container: 'sm:py-8 lg:py-8' }">
            <slot name="default" />
        </UPageSection>
    </TrangaPage>
</template>

<script setup lang="ts">
import { MangaCover, UPageCTA } from '#components';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';
import type { PageCTAProps, PageCTASlots } from '@nuxt/ui/components/PageCTA.vue';
import type { Manga } from '~/api/trangaApi';

export interface MangaPageProps extends PageCTAProps {
    manga?: Manga;
    actions?: (manga?: Manga) => ButtonProps[] | undefined;
}

const props = defineProps<MangaPageProps>();

defineSlots<PageCTASlots>();

const links = computed(() => {
    if (props.actions) return props.actions(props.manga);
    else return undefined;
});
</script>
