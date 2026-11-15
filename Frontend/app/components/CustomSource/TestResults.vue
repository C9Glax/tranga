<template>
    <UCard :ui="{ body: 'flex max-h-[60vh] flex-col gap-3 overflow-y-auto' }">
        <template #header>
            <h2 class="font-semibold">Test results</h2>
            <p v-if="test" class="text-sm break-all text-muted">{{ stepLabels[test.step] }} test for “{{ test.input }}”</p>
            <p v-else class="text-sm text-muted">What the selectors extract from the real website.</p>
        </template>

        <p v-if="!test" class="text-sm text-muted">
            Use the <strong>Test</strong> button {{ buttonAbove ? 'above' : 'of this step' }} to try it on the website. The result shows up
            here and stays visible while you change the selectors.
        </p>

        <div v-else-if="test.loading" class="flex flex-row items-center gap-2 text-sm text-muted">
            <UIcon name="i-lucide-loader-circle" class="animate-spin" />
            Running the {{ stepLabels[test.step].toLowerCase() }} test…
        </div>

        <UAlert
            v-else-if="test.errors.length"
            color="error"
            variant="subtle"
            icon="i-lucide-circle-alert"
            title="This step cannot be tested yet">
            <template #description>
                <ul class="list-disc pl-4">
                    <li v-for="error in test.errors" :key="error">{{ error }}</li>
                </ul>
            </template>
        </UAlert>

        <template v-else-if="test.result">
            <UAlert
                v-if="test.result.error"
                color="error"
                variant="subtle"
                icon="i-lucide-circle-alert"
                :title="test.result.error"
                :description="test.result.requestUrl ? `Requested: ${test.result.requestUrl}` : undefined"
                :ui="{ description: 'break-all' }" />
            <UAlert
                v-else
                color="success"
                variant="subtle"
                icon="i-lucide-check"
                :title="`${test.result.itemCount} item(s) extracted`"
                :description="`Requested: ${test.result.requestUrl} (HTTP ${test.result.statusCode})`"
                :ui="{ description: 'break-all' }" />

            <UAlert
                v-for="warning in test.result.warnings"
                :key="warning"
                color="warning"
                variant="subtle"
                icon="i-lucide-triangle-alert"
                :title="warning"
                :ui="{ title: 'break-words' }" />

            <ul v-if="rows.length" class="flex flex-col gap-2">
                <li v-for="(row, index) in rows" :key="index" class="flex flex-col gap-1.5 rounded-md border border-default p-2 text-sm">
                    <div v-for="(value, key) in row" :key="key" class="min-w-0">
                        <div class="text-xs font-medium text-muted capitalize">{{ key }}</div>
                        <a
                            v-if="isLink(key, value)"
                            :href="value!"
                            target="_blank"
                            rel="noopener noreferrer"
                            class="break-all text-primary underline-offset-2 hover:underline">
                            {{ value }}
                        </a>
                        <div v-else class="break-words">{{ value ?? '–' }}</div>
                    </div>
                    <UButton
                        v-if="pickLabel && isHttpUrl(row.url)"
                        :label="pickLabel"
                        icon="i-lucide-arrow-down-to-line"
                        color="neutral"
                        variant="subtle"
                        size="xs"
                        class="self-start"
                        @click="emit('pick', test.step, row.url!)" />
                </li>
            </ul>
            <p v-if="Number(test.result.itemCount) > rows.length" class="text-xs text-muted">
                Showing the first {{ rows.length }} of {{ test.result.itemCount }}.
            </p>
        </template>
    </UCard>
</template>

<script setup lang="ts">
type Row = Record<string, string | null>;

const props = defineProps<{
    test?: CustomSourceTestState;
    /** True when the Test button of the current step sits directly above these results (and not in the step itself). */
    buttonAbove?: boolean;
}>();
const emit = defineEmits<{ pick: [step: CustomSourceTestStep, url: string] }>();

const stepLabels: Record<CustomSourceTestStep, string> = { search: 'Search', chapters: 'Chapters', images: 'Pages' };

const rows = computed<Row[]>(() => (props.test?.result?.rows ?? []) as Row[]);

// Columns that hold links to the website or its images are shown as clickable links.
const LINK_COLUMNS = ['url', 'cover', 'page', 'image'];
const isHttpUrl = (value: string | null | undefined): boolean => typeof value === 'string' && /^https?:\/\//i.test(value);
const isLink = (key: string, value: string | null | undefined): boolean => LINK_COLUMNS.includes(key) && isHttpUrl(value);

// A search result becomes the series of the chapters test, a chapter the one of the pages test.
const pickLabel = computed(() =>
    props.test?.step === 'search' ? 'Use for chapters test' : props.test?.step === 'chapters' ? 'Use for pages test' : undefined,
);
</script>
