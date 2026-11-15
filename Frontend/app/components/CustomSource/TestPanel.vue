<template>
    <div class="flex flex-col gap-3 rounded-md border border-default p-3">
        <div class="flex flex-row flex-wrap items-end gap-2">
            <UFormField :label="inputLabel" :description="help" class="grow">
                <UInput v-model="input" :placeholder="inputPlaceholder" class="w-full" @keydown.enter.prevent="run" />
            </UFormField>
            <UButton label="Test" icon="i-lucide-flask-conical" color="neutral" :loading="loading" :disabled="!input.trim()" @click="run" />
        </div>

        <UAlert v-if="errors.length" color="error" variant="subtle" icon="i-lucide-circle-alert" title="This step cannot be tested yet">
            <template #description>
                <ul class="list-disc pl-4">
                    <li v-for="error in errors" :key="error">{{ error }}</li>
                </ul>
            </template>
        </UAlert>

        <template v-if="result">
            <UAlert
                v-if="result.error"
                color="error"
                variant="subtle"
                icon="i-lucide-circle-alert"
                :title="result.error"
                :description="result.requestUrl ? `Requested: ${result.requestUrl}` : undefined" />
            <UAlert
                v-else
                color="success"
                variant="subtle"
                icon="i-lucide-check"
                :title="`${result.itemCount} item(s) extracted`"
                :description="`Requested: ${result.requestUrl} (HTTP ${result.statusCode})`" />

            <UAlert
                v-for="warning in result.warnings"
                :key="warning"
                color="warning"
                variant="subtle"
                icon="i-lucide-triangle-alert"
                :title="warning" />

            <UTable v-if="rows.length" :data="rows" :columns="columns" :ui="{ td: 'max-w-xs truncate', th: 'capitalize' }" class="w-full" />
            <p v-if="Number(result.itemCount) > rows.length" class="text-xs text-muted">
                Showing the first {{ rows.length }} of {{ result.itemCount }}.
            </p>
        </template>
    </div>
</template>

<script setup lang="ts">
import { FetchError } from 'ofetch';
import type { TableColumn } from '@nuxt/ui/components/Table.vue';
import type { PostMangasCustomDownloadersTestResponse } from '~/api/tranga';

type Row = Record<string, string | null>;

const props = defineProps<{
    definition: CustomSourceDefinition;
    step: 'search' | 'chapters' | 'images';
    inputLabel: string;
    inputPlaceholder?: string;
    help?: string;
}>();

const input = ref('');
const loading = ref(false);
const result = ref<PostMangasCustomDownloadersTestResponse>();
const errors = ref<string[]>([]);

const rows = computed<Row[]>(() => (result.value?.rows ?? []) as Row[]);
const columns = computed<TableColumn<Row>[]>(() => Object.keys(rows.value[0] ?? {}).map((key) => ({ accessorKey: key, header: key })));

const run = async () => {
    if (!input.value.trim()) return;
    loading.value = true;
    result.value = undefined;
    errors.value = [];
    try {
        result.value = await useNuxtApp().$tranga<PostMangasCustomDownloadersTestResponse>('/mangas/customDownloaders/test', {
            method: 'post',
            body: { definition: customSourceForApi(props.definition), step: props.step, input: input.value.trim() },
        });
    } catch (error: unknown) {
        // 400: the API lists what is wrong with the definition (for this step)
        if (error instanceof FetchError && Array.isArray(error.data)) errors.value = error.data as string[];
        else errors.value = [error instanceof FetchError && typeof error.data === 'string' ? error.data : 'The test could not be run.'];
    } finally {
        loading.value = false;
    }
};
</script>
