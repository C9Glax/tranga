<template>
    <UPage>
        <UPageCTA v-bind="$props" :links="links" orientation="horizontal" reverse :ui="{ container: 'py-6 sm:py-8 lg:py-8' }">
            <template #title>
                <p v-if="$props.title">{{ $props.title }}</p>
                <p v-else-if="manga?.title">{{ manga.title }}</p>
                <USkeleton v-else class="h-lh" />
            </template>

            <!-- Passes through the slots to -->
            <template v-for="(_, slotName) in $slots" #[slotName]="slotProps">
                <slot v-if="slotName !== 'default'" :name="slotName as unknown" v-bind="slotProps" />
                <MangaCover v-else :file-id="coverFileId" :mangaId="manga?.mangaId" noBlur />
            </template>
            <MangaCover :file-id="coverFileId" :mangaId="manga?.mangaId" noBlur />
        </UPageCTA>

        <UPageSection :ui="{ container: 'sm:py-8 lg:py-8' }">
            <slot name="default" />
        </UPageSection>
    </UPage>
</template>

<script setup lang="ts">
import { MangaCover, UPageCTA } from '#components';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';
import type { MangaDto } from '~/api/trangaApi';
import type { PageCTAProps, PageCTASlots } from '@nuxt/ui/components/PageCTA.vue';

export interface MangaPageProps extends PageCTAProps {
    manga?: MangaDto;
    coverFileId?: string;
    actions?: (manga?: MangaDto) => ButtonProps[] | undefined;
}

const props = defineProps<MangaPageProps>();

defineSlots<PageCTASlots>();

const links = computed(() => (props.actions ? props.actions(props.manga) : undefined));
</script>
