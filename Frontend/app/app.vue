<template>
    <UApp :locale="uiLocale">
        <NuxtLayout>
            <NuxtPage />
        </NuxtLayout>
        <ClientOnly>
            <TourGuide />
        </ClientOnly>
    </UApp>
</template>

<script setup lang="ts">
import { TourGuide } from '#components';
import * as uiLocales from '@nuxt/ui/locale';

const { locale } = useI18n();

// Keeps <html lang> and <html dir> in sync with the selected language.
const localeHead = useLocaleHead();
useHead({ htmlAttrs: { lang: () => localeHead.value.htmlAttrs?.lang, dir: () => localeHead.value.htmlAttrs?.dir } });

// Nuxt UI ships the strings of its own components (pagination, date pickers, ...) per language. Its locale names use
// underscores and lowercase (pt-BR -> pt_br); a language Nuxt UI does not know falls back to English.
const uiLocale = computed(
    () => (uiLocales as Record<string, (typeof uiLocales)['en']>)[locale.value.replace('-', '_').toLowerCase()] ?? uiLocales.en,
);
</script>
