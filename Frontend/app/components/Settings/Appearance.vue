<template>
    <UPageList class="gap-4">
        <UCard>
            <template #header>
                <p class="font-semibold">{{ $t('settings.appearance.language.title') }}</p>
            </template>

            <!-- Each language is named in its own language, so it can be found even when the current one is unreadable. -->
            <UFormField :label="$t('settings.appearance.language.legend')">
                <USelectMenu v-model="selectedLocale" :items="localeItems" value-key="value" :search-input="false" class="w-full sm:w-64" />
            </UFormField>
        </UCard>

        <UCard>
            <template #header>
                <p class="font-semibold">{{ $t('settings.appearance.nsfwCovers.title') }}</p>
            </template>

            <URadioGroup v-model="nsfwCoverMode" :legend="$t('settings.appearance.nsfwCovers.legend')" :items="options" value-key="value" />
        </UCard>
    </UPageList>
</template>

<script setup lang="ts">
import type { RadioGroupItem } from '@nuxt/ui';

const { t, locale, locales, setLocale } = useI18n();

const localeItems = computed(() => locales.value.map((l) => ({ value: l.code, label: l.name ?? l.code })));

const selectedLocale = computed({ get: () => locale.value, set: (code) => void setLocale(code) });

const nsfwCoverMode = useNsfwCoverMode();

const options = computed((): (RadioGroupItem & { value: NsfwCoverMode })[] => [
    {
        value: 'show',
        label: t('settings.appearance.nsfwCovers.show.label'),
        description: t('settings.appearance.nsfwCovers.show.description'),
    },
    {
        value: 'click',
        label: t('settings.appearance.nsfwCovers.click.label'),
        description: t('settings.appearance.nsfwCovers.click.description'),
    },
    {
        value: 'hide',
        label: t('settings.appearance.nsfwCovers.hide.label'),
        description: t('settings.appearance.nsfwCovers.hide.description'),
    },
]);
</script>
