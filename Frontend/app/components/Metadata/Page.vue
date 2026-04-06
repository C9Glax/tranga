<template>
    <TrangaPage :page-title="{ title: 'Metadata', icon: { name: 'i-lucide-info', color: 'info' } }">
        <UPageCTA
            v-bind="$props"
            :links="links"
            orientation="horizontal"
            reverse
            :ui="{ container: 'py-6 sm:py-8 lg:py-8' }"
            class="w-full h-max">
            <template #title>
                <p v-if="$props.title">{{ $props.title }}</p>
                <p v-else-if="metadata?.series">{{ metadata?.series }}</p>
                <USkeleton v-else class="h-lh" />
            </template>

            <template #description>
                <p v-if="$props.title">{{ $props.title }}</p>
                <UEditor
                    v-else-if="metadata"
                    v-model="metadata.summary"
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
                <MangaCover v-else :file-id="metadata?.coverId" :noBlur="!metadata?.nsfw" />
            </template>
            <MangaCover :file-id="metadata?.coverId" :noBlur="!metadata?.nsfw" />
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
import type { Metadata } from '~/api/trangaApi';

export interface MangaPageProps extends PageCTAProps {
    metadata?: Metadata;
    actions?: (metadata?: Metadata) => ButtonProps[] | undefined;
}

const props = defineProps<MangaPageProps>();

defineSlots<PageCTASlots>();

const links = computed(() => {
    if (props.actions) return props.actions(props.metadata);
    else return undefined;
});
</script>
