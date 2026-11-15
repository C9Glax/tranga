<template>
    <UPageList class="gap-4">
        <UCard>
            <template #header>
                <p class="font-semibold">NSFW covers</p>
            </template>

            <URadioGroup
                v-model="nsfwCoverMode"
                legend="How should covers of NSFW manga be displayed?"
                :items="options"
                value-key="value" />
        </UCard>
    </UPageList>
</template>

<script setup lang="ts">
import type { RadioGroupItem } from '@nuxt/ui';

const nsfwCoverMode = useNsfwCoverMode();

const options: (RadioGroupItem & { value: NsfwCoverMode })[] = [
    { value: 'show', label: 'Always visible', description: 'NSFW covers are shown like any other cover.' },
    { value: 'click', label: 'Visible on click', description: 'NSFW covers are blurred until you click them.' },
    { value: 'hide', label: 'Never visible', description: 'NSFW covers always stay blurred.' },
];
</script>
