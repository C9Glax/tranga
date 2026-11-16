<template>
    <UCard variant="outline" :ui="{ body: 'p-3 sm:p-3' }">
        <UFormField :label="inputLabel" :description="help">
            <UFieldGroup class="w-full">
                <UInput v-model="input" :placeholder="inputPlaceholder" class="grow" @keydown.enter.prevent="run" />
                <UButton
                    label="Test"
                    icon="i-lucide-flask-conical"
                    color="neutral"
                    :loading="test?.loading && test.step === step"
                    :disabled="!input.trim()"
                    @click="run" />
            </UFieldGroup>
        </UFormField>
    </UCard>
</template>

<script setup lang="ts">
import type { PostMangasCustomDownloadersTestResponse } from '~/api/tranga';

const props = defineProps<{
    definition: CustomSourceDefinition;
    step: CustomSourceTestStep;
    inputLabel: string;
    inputPlaceholder?: string;
    help?: string;
}>();

/** What is tried: a search text, a series URL or a chapter URL. */
const input = defineModel<string>('input', { default: '' });
/** The latest test, shown by the results next to the steps. */
const test = defineModel<CustomSourceTestState | undefined>('test');

const run = async () => {
    const value = input.value.trim();
    if (!value) return;
    test.value = { step: props.step, input: value, loading: true, errors: [] };
    try {
        const result = await useNuxtApp().$tranga<PostMangasCustomDownloadersTestResponse>('/mangas/customDownloaders/test', {
            method: 'post',
            body: customSourceTestBody(props.definition, props.step, value),
        });
        test.value = { step: props.step, input: value, loading: false, result, errors: [] };
    } catch (error: unknown) {
        // 400: the API lists what is wrong with the settings or with the section of this step
        test.value = { step: props.step, input: value, loading: false, errors: apiErrorMessages(error, 'The test could not be run.') };
    }
};
</script>
