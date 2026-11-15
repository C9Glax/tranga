<template>
    <!-- shrink-0: with long results the alerts must not be squashed to make room, the list scrolls instead -->
    <UCard :ui="{ body: 'flex max-h-[60vh] flex-col gap-3 overflow-y-auto [&>*]:shrink-0' }">
        <template #header>
            <h2 class="font-semibold">Test results</h2>
            <p v-if="test" class="text-sm break-all text-muted">{{ stepLabels[test.step] }} test for “{{ test.input }}”</p>
            <p v-else class="text-sm text-muted">What the selectors extract from the real website.</p>
        </template>

        <UEmpty
            v-if="!test"
            icon="i-lucide-flask-conical"
            title="Nothing tested yet"
            description="Use the Test button above to try it on the website. The result shows up here and stays visible while you change the selectors."
            variant="naked"
            size="sm" />

        <!-- The same shape as the finished result: an alert and a couple of result cards. -->
        <div v-else-if="test.loading" class="flex flex-col gap-3" role="status" aria-live="polite">
            <p class="text-sm text-muted">Running the {{ stepLabels[test.step].toLowerCase() }} test…</p>
            <USkeleton class="h-14 w-full" />
            <UCard v-for="n in 2" :key="n" variant="subtle" :ui="{ body: 'p-3 sm:p-3' }">
                <div class="flex flex-col gap-2">
                    <USkeleton class="h-3 w-12" />
                    <USkeleton class="h-4 w-40" />
                    <USkeleton class="h-3 w-12" />
                    <USkeleton class="h-4 w-full" />
                </div>
            </UCard>
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

            <div v-if="rows.length" class="flex flex-col gap-2">
                <UCard v-for="(row, index) in rows" :key="index" variant="subtle" :ui="{ body: 'p-3 sm:p-3', footer: 'p-3 sm:p-3' }">
                    <dl class="flex flex-col gap-1.5 text-sm">
                        <div v-for="(value, key) in row" :key="key" class="min-w-0">
                            <dt class="text-xs font-medium text-muted capitalize">{{ key }}</dt>
                            <dd>
                                <a
                                    v-if="isLink(key, value)"
                                    :href="value!"
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    class="break-all text-primary underline-offset-2 hover:underline">
                                    {{ value }}
                                </a>
                                <span v-else class="break-words">{{ value ?? '–' }}</span>
                            </dd>
                        </div>
                    </dl>
                    <template v-if="pickLabel && isHttpUrl(row.url)" #footer>
                        <UButton
                            :label="pickLabel"
                            icon="i-lucide-arrow-down-to-line"
                            color="neutral"
                            variant="subtle"
                            size="xs"
                            @click="emit('pick', test.step, row.url!)" />
                    </template>
                </UCard>
            </div>
            <p v-if="Number(test.result.itemCount) > rows.length" class="text-xs text-muted">
                Showing the first {{ rows.length }} of {{ test.result.itemCount }}.
            </p>
        </template>
    </UCard>
</template>

<script setup lang="ts">
type Row = Record<string, string | null>;

const props = defineProps<{ test?: CustomSourceTestState }>();
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
