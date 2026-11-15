<template>
    <div class="flex flex-row flex-wrap items-end gap-2 rounded-md border border-default p-3">
        <UFormField :label="inputLabel" :description="help" class="grow">
            <UInput v-model="input" :placeholder="inputPlaceholder" class="w-full" @keydown.enter.prevent="run" />
        </UFormField>
        <UButton
            label="Test"
            icon="i-lucide-flask-conical"
            color="neutral"
            :loading="test?.loading && test.step === step"
            :disabled="!input.trim()"
            @click="run" />
    </div>
</template>

<script setup lang="ts">
import { FetchError } from 'ofetch';
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
            body: { definition: customSourceForApi(props.definition), step: props.step, input: value },
        });
        test.value = { step: props.step, input: value, loading: false, result, errors: [] };
    } catch (error: unknown) {
        // 400: the API lists what is wrong with the definition (for this step)
        const errors =
            error instanceof FetchError && Array.isArray(error.data)
                ? (error.data as string[])
                : [error instanceof FetchError && typeof error.data === 'string' ? error.data : 'The test could not be run.'];
        test.value = { step: props.step, input: value, loading: false, errors };
    }
};
</script>
