<template>
    <!-- shrink-0: with long results the alerts must not be squashed to make room, the list scrolls instead -->
    <UCard :ui="{ body: 'flex max-h-[60vh] flex-col gap-3 overflow-y-auto [&>*]:shrink-0' }">
        <template #header>
            <h2 class="font-semibold">{{ $t('customSource.results.title') }}</h2>
            <p v-if="test" class="text-sm break-all text-muted">
                {{ $t('customSource.results.testFor', { step: stepLabels[test.step], input: test.input }) }}
            </p>
            <p v-else class="text-sm text-muted">{{ $t('customSource.results.idle') }}</p>
        </template>

        <UEmpty
            v-if="!test"
            icon="i-lucide-flask-conical"
            :title="$t('customSource.results.emptyTitle')"
            :description="$t('customSource.results.emptyDescription')"
            variant="naked"
            size="sm" />

        <!-- The same shape as the finished result: an alert and a couple of result cards. -->
        <div v-else-if="test.loading" class="flex flex-col gap-3" role="status" aria-live="polite">
            <p class="text-sm text-muted">{{ $t(`customSource.results.running.${test.step}`) }}</p>
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
            :title="$t('customSource.results.cannotTest')">
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
                :description="test.result.requestUrl ? $t('customSource.results.requested', { url: test.result.requestUrl }) : undefined"
                :ui="{ description: 'break-all' }" />
            <UAlert
                v-else
                color="success"
                variant="subtle"
                icon="i-lucide-check"
                :title="$t('customSource.results.extracted', Number(test.result.itemCount))"
                :description="
                    $t('customSource.results.requestedWithStatus', { url: test.result.requestUrl, status: test.result.statusCode })
                "
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
                                    v-if="LINK_COLUMNS.includes(key) && isHttpUrl(value)"
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
                {{ $t('customSource.results.showing', { shown: rows.length, total: test.result.itemCount }) }}
            </p>
        </template>
    </UCard>
</template>

<script setup lang="ts">
type Row = Record<string, string | null>;

const { t } = useI18n();

const props = defineProps<{ test?: CustomSourceTestState }>();
const emit = defineEmits<{ pick: [step: CustomSourceTestStep, url: string] }>();

const stepLabels = computed((): Record<CustomSourceTestStep, string> => ({
    search: t('customSource.results.steps.search'),
    series: t('customSource.results.steps.series'),
    chapters: t('customSource.results.steps.chapters'),
    images: t('customSource.results.steps.images'),
}));

const rows = computed<Row[]>(() => (props.test?.result?.rows ?? []) as Row[]);

// Columns that hold links to the website or its images are shown as clickable links (when the value really is one).
const LINK_COLUMNS = ['url', 'cover', 'page', 'image'];
const isHttpUrl = (value: string | null | undefined): boolean => typeof value === 'string' && /^https?:\/\//i.test(value);

// A search result becomes the series of the chapters test, a chapter the one of the pages test.
const pickLabel = computed(() =>
    props.test?.step === 'search'
        ? t('customSource.results.useForChapters')
        : props.test?.step === 'chapters'
          ? t('customSource.results.useForPages')
          : undefined,
);
</script>
