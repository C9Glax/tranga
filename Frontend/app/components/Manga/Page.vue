<template>
    <UPage>
        <UPageCTA :links="links" orientation="horizontal" reverse :ui="{ container: 'py-6 sm:py-8 lg:py-8' }">
            <template #title>
                <USkeleton v-if="!manga?.title" class="h-lh" />
                <p v-else>{{ manga.title }}</p>
            </template>
            <MangaCover :mangaId="manga?.mangaId" noBlur />
        </UPageCTA>
        <UPageSection :ui="{ container: 'sm:py-8 lg:py-8' }">
            <slot />
        </UPageSection>
    </UPage>
</template>

<script setup lang="ts">
import { MangaCover } from '#components';
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';
import type { MangaDto } from '~/api/trangaApi';

const manga = defineModel<MangaDto>();

const props = defineProps<{ actions?: (manga?: MangaDto) => ButtonProps[] | undefined }>();

const links = computed(() => (props.actions ? props.actions(manga.value) : undefined));
</script>
