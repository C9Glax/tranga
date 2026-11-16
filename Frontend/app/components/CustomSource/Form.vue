<template>
    <div class="flex flex-col gap-4">
        <div class="flex flex-row flex-wrap gap-2">
            <UDropdownMenu v-if="examples?.length" :items="exampleItems">
                <UButton
                    :label="$t('customSource.form.examples')"
                    icon="i-lucide-wand-sparkles"
                    color="neutral"
                    variant="subtle"
                    trailing-icon="i-lucide-chevron-down" />
            </UDropdownMenu>
            <UButton :label="$t('customSource.form.showJson')" icon="i-lucide-braces" color="neutral" variant="subtle" @click="openJson" />
            <UButton
                :label="$t('customSource.form.importJson')"
                icon="i-lucide-file-input"
                color="neutral"
                variant="subtle"
                @click="openImport" />
        </div>

        <div class="grid grid-cols-1 gap-4 lg:grid-cols-3">
            <UForm :state="state" class="flex min-w-0 flex-col gap-4 lg:col-span-2" @submit="submit">
                <UStepper ref="stepper" v-model="step" :items="steps" :linear="false" class="w-full">
                    <template #general>
                        <div class="flex flex-col gap-4 pt-4">
                            <UAlert
                                color="neutral"
                                variant="subtle"
                                icon="i-lucide-info"
                                :title="texts.introTitle"
                                :description="$t('customSource.form.introWithHint', { intro: texts.intro })"
                                :actions="[
                                    {
                                        label: $t('customSource.form.promptForAi'),
                                        icon: 'i-lucide-bot',
                                        to: agentPromptUrl,
                                        target: '_blank',
                                        color: 'neutral',
                                        variant: 'outline',
                                    },
                                ]" />
                            <UFormField :label="$t('common.name')" name="name" required>
                                <UInput v-model="state.name" :placeholder="$t('customSource.form.namePlaceholder')" class="w-full" />
                            </UFormField>
                            <UFormField :label="$t('common.baseUrl')" name="baseUrl" required>
                                <UInput
                                    v-model="state.baseUrl"
                                    placeholder="https://example.com"
                                    class="w-full"
                                    @blur="state.baseUrl = normalizeBaseUrl(state.baseUrl)" />
                            </UFormField>
                            <UFormField
                                :label="$t('customSource.form.responseFormat')"
                                name="format"
                                required
                                :description="$t('customSource.form.responseFormatDescription')">
                                <URadioGroup
                                    v-model="state.format"
                                    :items="formatItems"
                                    variant="card"
                                    :ui="{ fieldset: 'grid grid-cols-1 gap-2 sm:grid-cols-2' }"
                                    class="w-full" />
                            </UFormField>
                            <UFormField
                                :label="$t('customSource.form.iconUrl')"
                                name="iconUrl"
                                :description="$t('customSource.form.iconUrlDescription')">
                                <UInput v-model="state.iconUrl" placeholder="https://example.com/icon.png" class="w-full" />
                            </UFormField>
                            <UFormField
                                :label="$t('customSource.form.languages')"
                                name="languages"
                                required
                                :error="languageError"
                                :description="$t('customSource.form.languagesDescription')">
                                <UInputTags v-model="languages" placeholder="en" class="w-full" />
                            </UFormField>
                            <UFormField
                                :label="$t('customSource.form.seriesUrlPattern')"
                                name="seriesUrlPattern"
                                :description="$t('customSource.form.seriesUrlPatternDescription', { group: '(?<id>[^/]+)', id: '{id}' })">
                                <!-- measured, so the pattern test in the sidebar can start at the same height -->
                                <div ref="patternInput">
                                    <UInput
                                        v-model="state.seriesUrlPattern"
                                        placeholder="^https://example\.com/series/(?<id>[^/]+)"
                                        :ui="{ base: 'font-mono' }"
                                        class="w-full" />
                                </div>
                            </UFormField>
                            <div class="flex flex-row flex-wrap gap-6">
                                <UFormField
                                    :label="$t('customSource.form.requestsPerSecond')"
                                    name="requestsPerSecond"
                                    :description="$t('customSource.form.requestsPerSecondDescription')">
                                    <UInputNumber
                                        v-model="state.requestsPerSecond"
                                        :min="0.1"
                                        :max="20"
                                        :step="0.1"
                                        :format-options="{ maximumFractionDigits: 2 }" />
                                </UFormField>
                                <UFormField
                                    :label="$t('customSource.form.flareSolverr')"
                                    name="useFlareSolverr"
                                    :description="$t('customSource.form.flareSolverrDescription')">
                                    <!-- Only explains itself while the switch is disabled. The wrapper keeps hovering working on a disabled switch. -->
                                    <UTooltip :text="$t('customSource.form.flareSolverrMissing')" :disabled="!flareSolverrMissing">
                                        <span class="inline-flex">
                                            <USwitch
                                                v-model="state.useFlareSolverr"
                                                :disabled="flareSolverrMissing"
                                                :label="$t('customSource.form.useFlareSolverr')" />
                                        </span>
                                    </UTooltip>
                                </UFormField>
                            </div>
                            <UFormField
                                :label="$t('customSource.form.headers')"
                                name="headers"
                                :description="$t('customSource.form.headersDescription')">
                                <div class="flex flex-col gap-2">
                                    <div v-for="(row, index) in headerRows" :key="index" class="flex flex-row gap-2">
                                        <UInput v-model="row.name" :placeholder="$t('customSource.form.headerName')" class="w-56" />
                                        <UInput v-model="row.value" :placeholder="$t('customSource.form.headerValue')" class="grow" />
                                        <UButton
                                            icon="i-lucide-trash-2"
                                            color="neutral"
                                            variant="ghost"
                                            :aria-label="$t('customSource.form.removeHeader')"
                                            @click="headerRows.splice(index, 1)" />
                                    </div>
                                    <div>
                                        <UButton
                                            :label="$t('customSource.form.addHeader')"
                                            icon="i-lucide-plus"
                                            color="neutral"
                                            variant="subtle"
                                            size="sm"
                                            @click="headerRows.push({ name: '', value: '' })" />
                                    </div>
                                </div>
                            </UFormField>
                        </div>
                    </template>

                    <template #search>
                        <div class="flex flex-col gap-4 pt-4">
                            <p class="text-sm text-muted">{{ $t('customSource.search.intro') }}</p>
                            <UFormField
                                :label="$t('customSource.search.urlLabel')"
                                name="search.urlTemplate"
                                required
                                :description="searchUrlDescription">
                                <UInput
                                    v-model="state.search.urlTemplate"
                                    :placeholder="texts.searchUrlPlaceholder"
                                    :ui="{ base: 'font-mono' }"
                                    class="w-full" />
                            </UFormField>
                            <UFormField :label="texts.searchItemLabel" name="search.itemSelector" required :description="texts.searchItem">
                                <UInput
                                    v-model="state.search.itemSelector"
                                    :placeholder="texts.searchItemPlaceholder"
                                    :ui="{ base: 'font-mono' }"
                                    class="w-full" />
                            </UFormField>
                            <CustomSourceFieldInput
                                v-model="state.search.title"
                                :format="state.format"
                                :label="$t('customSource.search.title')"
                                required
                                :json-placeholder="$t('customSource.example', { value: 'title' })" />
                            <CustomSourceFieldInput
                                v-model="state.search.url"
                                :format="state.format"
                                :label="$t('customSource.search.link')"
                                required
                                :description="texts.link"
                                :attribute-placeholder="$t('customSource.example', { value: 'href' })"
                                :json-placeholder="$t('customSource.exampleEither', { first: 'url', second: 'slug' })" />
                            <CustomSourceFieldInput
                                v-model="state.search.cover"
                                :format="state.format"
                                :label="$t('customSource.search.cover')"
                                required
                                :attribute-placeholder="$t('customSource.example', { value: 'data-src,src' })"
                                :json-placeholder="$t('customSource.example', { value: 'cover' })"
                                :description="texts.cover" />
                        </div>
                    </template>

                    <template #series>
                        <div class="flex flex-col gap-4 pt-4">
                            <p class="text-sm text-muted">{{ $t('customSource.series.intro') }}</p>
                            <UFormField
                                :label="$t('customSource.series.urlLabel')"
                                name="series.urlTemplate"
                                :description="$t('customSource.series.urlDescription', { url: '{url}', example: '/api/manga/{slug}' })">
                                <UInput v-model="state.series.urlTemplate" placeholder="{url}" :ui="{ base: 'font-mono' }" class="w-full" />
                            </UFormField>
                            <CustomSourceFieldInput
                                v-model="state.series.cover"
                                :format="state.format"
                                :label="$t('customSource.series.cover')"
                                :description="texts.seriesCover"
                                :attribute-placeholder="$t('customSource.example', { value: 'src' })"
                                :json-placeholder="$t('customSource.example', { value: 'manga.cover' })" />
                        </div>
                    </template>

                    <template #chapters>
                        <div class="flex flex-col gap-4 pt-4">
                            <p class="text-sm text-muted">{{ $t('customSource.chapters.intro') }}</p>
                            <UFormField
                                :label="$t('customSource.chapters.urlLabel')"
                                name="chapters.urlTemplate"
                                required
                                :description="
                                    $t('customSource.chapters.urlDescription', {
                                        url: '{url}',
                                        example: 'https://example.com/series/{id}/chapters',
                                    })
                                ">
                                <UInput
                                    v-model="state.chapters.urlTemplate"
                                    placeholder="{url}"
                                    :ui="{ base: 'font-mono' }"
                                    class="w-full" />
                            </UFormField>
                            <UFormField
                                :label="texts.chapterItemLabel"
                                name="chapters.itemSelector"
                                required
                                :description="texts.chapterItem">
                                <UInput
                                    v-model="state.chapters.itemSelector"
                                    :placeholder="texts.chapterItemPlaceholder"
                                    :ui="{ base: 'font-mono' }"
                                    class="w-full" />
                            </UFormField>
                            <CustomSourceFieldInput
                                v-model="state.chapters.url"
                                :format="state.format"
                                :label="$t('customSource.chapters.link')"
                                required
                                :description="texts.link"
                                :attribute-placeholder="$t('customSource.example', { value: 'href' })"
                                :json-placeholder="$t('customSource.exampleEither', { first: 'url', second: 'id' })" />
                            <CustomSourceFieldInput
                                v-model="state.chapters.number"
                                :format="state.format"
                                :label="$t('customSource.chapters.number')"
                                :required="isJson"
                                :description="texts.chapterNumber"
                                :json-placeholder="$t('customSource.example', { value: 'chapter_number' })" />
                            <CustomSourceFieldInput
                                v-model="state.chapters.title"
                                :format="state.format"
                                :label="$t('customSource.chapters.title')"
                                :description="$t('customSource.optional')"
                                :json-placeholder="$t('customSource.example', { value: 'title' })" />
                            <CustomSourceFieldInput
                                v-model="state.chapters.date"
                                :format="state.format"
                                :label="$t('customSource.chapters.date')"
                                :description="$t('customSource.optional')"
                                :attribute-placeholder="$t('customSource.example', { value: 'datetime' })"
                                :json-placeholder="$t('customSource.example', { value: 'published_at' })" />
                            <UFormField
                                :label="$t('customSource.chapters.dateFormat')"
                                name="chapters.dateFormat"
                                :description="$t('customSource.chapters.dateFormatDescription')">
                                <UInput
                                    v-model="state.chapters.dateFormat"
                                    placeholder="yyyy-MM-dd"
                                    :ui="{ base: 'font-mono' }"
                                    class="w-full" />
                            </UFormField>
                            <CustomSourceFieldInput
                                v-model="state.chapters.nextPage"
                                :format="state.format"
                                :label="$t('customSource.chapters.nextPage')"
                                :description="$t('customSource.chapters.nextPageDescription')"
                                :attribute-placeholder="$t('customSource.example', { value: 'href' })"
                                :json-placeholder="$t('customSource.example', { value: 'next' })" />
                        </div>
                    </template>

                    <template #pages>
                        <div class="flex flex-col gap-4 pt-4">
                            <p class="text-sm text-muted">{{ $t('customSource.pages.intro') }}</p>
                            <UFormField
                                :label="$t('customSource.pages.urlLabel')"
                                name="images.urlTemplate"
                                required
                                :description="
                                    $t('customSource.pages.urlDescription', {
                                        url: '{url}',
                                        example1: '{url}/images',
                                        example2: '/api/chapter/{id}',
                                    })
                                ">
                                <UInput v-model="state.images.urlTemplate" placeholder="{url}" :ui="{ base: 'font-mono' }" class="w-full" />
                            </UFormField>
                            <UFormField
                                :label="$t('customSource.pages.urlPattern')"
                                name="images.urlPattern"
                                :description="$t('customSource.pages.urlPatternDescription', { group: '(?<id>\\d+)', id: '{id}' })">
                                <UInput
                                    v-model="state.images.urlPattern"
                                    placeholder="/chapter/(?<id>\d+)"
                                    :ui="{ base: 'font-mono' }"
                                    class="w-full" />
                            </UFormField>
                            <CustomSourceFieldInput
                                v-model="state.images.pageSelector"
                                :format="state.format"
                                :json-placeholder="$t('customSource.example', { value: 'pages[*].url' })"
                                :label="$t('customSource.pages.pageLinks')"
                                :description="texts.pageLinks"
                                selector-placeholder="e.g. select.pages option"
                                :attribute-placeholder="$t('customSource.defaultValue', { value: 'href,value' })"
                                :attribute-hint="$t('customSource.pages.pageLinksAttributeHint')" />
                            <UFormField :label="texts.imageLabel" name="images.imageSelector" required :description="texts.image">
                                <UInput
                                    v-model="state.images.imageSelector"
                                    :placeholder="texts.imagePlaceholder"
                                    :ui="{ base: 'font-mono' }"
                                    class="w-full" />
                            </UFormField>
                            <UFormField
                                v-if="!isJson"
                                :label="$t('customSource.pages.imageAttribute')"
                                name="images.attribute"
                                :description="$t('customSource.pages.imageAttributeDescription', { value: 'data-src,data-lazy-src,src' })">
                                <UInput
                                    v-model="state.images.attribute"
                                    placeholder="data-src,src"
                                    :ui="{ base: 'font-mono' }"
                                    class="w-full" />
                            </UFormField>
                        </div>
                    </template>
                </UStepper>

                <UAlert
                    v-if="errors.length"
                    color="error"
                    variant="subtle"
                    icon="i-lucide-circle-alert"
                    :title="$t('customSource.errors.saveFailedTitle')">
                    <template #description>
                        <ul class="list-disc pl-4">
                            <li v-for="error in errors" :key="error">{{ error }}</li>
                        </ul>
                    </template>
                </UAlert>

                <div class="flex flex-row flex-wrap items-center justify-between gap-2">
                    <UButton
                        :label="$t('common.back')"
                        icon="i-lucide-arrow-left"
                        color="neutral"
                        variant="outline"
                        :disabled="!stepper?.hasPrev"
                        @click="stepper?.prev()" />
                    <div class="flex flex-row flex-wrap justify-end gap-2">
                        <UButton :label="$t('common.cancel')" color="neutral" variant="ghost" to="/settings/sources" />
                        <UButton
                            :label="$t('common.next')"
                            trailing-icon="i-lucide-arrow-right"
                            color="neutral"
                            variant="outline"
                            :disabled="!stepper?.hasNext"
                            @click="stepper?.next()" />
                        <UButton
                            v-if="isLastStep"
                            type="submit"
                            :label="submitLabel ?? $t('common.save')"
                            icon="i-lucide-save"
                            :loading="saving" />
                    </div>
                </div>
            </UForm>

            <!-- The sidebar: on the General step the test of the series URL pattern (level with the pattern field), on the
                 other steps the latest test result of that step. -->
            <div ref="sidebar" class="min-w-0 lg:col-span-1" :style="{ paddingTop: `${isGeneralStep ? patternOffset : 0}px` }">
                <div v-if="isGeneralStep" class="flex flex-col gap-2">
                    <UInput
                        v-model="testUrls.pattern"
                        :aria-label="$t('customSource.form.patternTry')"
                        :placeholder="$t('customSource.form.patternTry')"
                        class="w-full" />
                    <div v-if="patternResult.state !== 'empty'" class="flex flex-row flex-wrap items-center gap-2 text-sm">
                        <template v-if="patternResult.state === 'invalid'">
                            <UIcon name="i-lucide-circle-alert" class="text-error" />
                            <span class="text-error">{{ patternResult.message }}</span>
                        </template>
                        <template v-else-if="patternResult.state === 'no-match'">
                            <UIcon name="i-lucide-x" class="text-warning" />
                            <span class="text-warning">{{ $t('customSource.form.patternNoMatch') }}</span>
                        </template>
                        <template v-else>
                            <UIcon name="i-lucide-check" class="text-success" />
                            <span class="text-success">{{ $t('customSource.form.patternMatches') }}</span>
                            <template v-if="patternResult.groups.length">
                                <UBadge
                                    v-for="group in patternResult.groups"
                                    :key="group.name"
                                    color="neutral"
                                    variant="subtle"
                                    class="font-mono">
                                    <!-- shown the way the group is written in the chapter list URL: {name} -->
                                    {{ '{' + group.name + '}' }} = {{ group.value || $t('customSource.form.patternEmptyGroup') }}
                                </UBadge>
                            </template>
                            <span v-else class="text-muted">{{ $t('customSource.form.patternNoGroups') }}</span>
                            <UButton
                                :label="$t('customSource.form.useForChaptersTest')"
                                icon="i-lucide-arrow-down-to-line"
                                color="neutral"
                                variant="subtle"
                                size="xs"
                                @click="useSeriesUrl(testUrls.pattern.trim())" />
                        </template>
                    </div>
                </div>
                <div v-else class="flex flex-col gap-4 lg:sticky lg:top-4">
                    <!-- Every step is tried from here, right above its result. -->
                    <CustomSourceTestPanel
                        v-if="step === 'search'"
                        v-model:input="testUrls.search"
                        v-model:test="tests.search"
                        :definition="state"
                        step="search"
                        :input-label="$t('customSource.search.testLabel')"
                        :input-placeholder="$t('customSource.search.testPlaceholder')" />
                    <CustomSourceTestPanel
                        v-else-if="step === 'series'"
                        v-model:input="testUrls.series"
                        v-model:test="tests.series"
                        :definition="state"
                        step="series"
                        :input-label="$t('customSource.series.testLabel')"
                        input-placeholder="https://example.com/series/1"
                        :help="$t('customSource.series.testHelp')" />
                    <CustomSourceTestPanel
                        v-else-if="step === 'chapters'"
                        v-model:input="testUrls.chapters"
                        v-model:test="tests.chapters"
                        :definition="state"
                        step="chapters"
                        :input-label="$t('customSource.chapters.testLabel')"
                        input-placeholder="https://example.com/series/1"
                        :help="$t('customSource.chapters.testHelp')" />
                    <CustomSourceTestPanel
                        v-else-if="step === 'pages'"
                        v-model:input="testUrls.images"
                        v-model:test="tests.images"
                        :definition="state"
                        step="images"
                        :input-label="$t('customSource.pages.testLabel')"
                        input-placeholder="https://example.com/chapter/1"
                        :help="$t('customSource.pages.testHelp')" />
                    <CustomSourceTestResults :test="currentTest" @pick="pickFromResults" />
                </div>
            </div>
        </div>

        <UModal v-model:open="jsonOpen" :title="$t('customSource.form.jsonTitle')" :description="$t('customSource.form.jsonDescription')">
            <template #body>
                <UTextarea :model-value="json" readonly :rows="18" :ui="{ base: 'font-mono text-xs' }" class="w-full" />
            </template>
            <template #footer>
                <div class="flex w-full flex-row justify-end gap-2">
                    <UButton :label="$t('customSource.form.close')" color="neutral" variant="ghost" @click="jsonOpen = false" />
                    <UButton :label="$t('customSource.form.copy')" icon="i-lucide-copy" @click="copyJson" />
                </div>
            </template>
        </UModal>

        <UModal
            v-model:open="importOpen"
            :title="$t('customSource.form.importTitle')"
            :description="$t('customSource.form.importDescription')">
            <template #body>
                <div class="flex flex-col gap-3">
                    <UTextarea
                        v-model="importText"
                        :rows="18"
                        placeholder='{ "name": "…", … }'
                        :ui="{ base: 'font-mono text-xs' }"
                        class="w-full" />
                    <UAlert v-if="importError" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="importError" />
                </div>
            </template>
            <template #footer>
                <div class="flex w-full flex-row justify-end gap-2">
                    <UButton :label="$t('common.cancel')" color="neutral" variant="ghost" @click="importOpen = false" />
                    <UButton
                        :label="$t('customSource.form.import')"
                        icon="i-lucide-file-input"
                        :disabled="!importText.trim()"
                        @click="importJson" />
                </div>
            </template>
        </UModal>
    </div>
</template>

<script setup lang="ts">
import type { DropdownMenuItem } from '@nuxt/ui/components/DropdownMenu.vue';
import type { StepperItem } from '@nuxt/ui/components/Stepper.vue';
import type { GetMangasCustomDownloadersCapabilitiesResponse, GetMangasCustomDownloadersExamplesResponse } from '~/api/tranga';
import { useResizeObserver } from '@vueuse/core';
import { ApiKeys } from '~/composables/ApiKeys';

const props = withDefaults(
    defineProps<{ initial: CustomSourceDefinition; submitLabel?: string; saving?: boolean; errors?: string[]; showExamples?: boolean }>(),
    { saving: false, errors: () => [], showExamples: false },
);
const emit = defineEmits<{ submit: [definition: CustomSourceDefinition] }>();

const { t } = useI18n();
const toast = useToast();

const state = ref<CustomSourceDefinition>(normalizeCustomSource(props.initial));
const headerRows = ref<{ name: string; value: string }[]>([]);

const loadState = (definition: CustomSourceDefinition) => {
    state.value = definition;
    headerRows.value = Object.entries(definition.headers).map(([name, value]) => ({ name, value }));
};
loadState(state.value);

// ----------------------------------------------------------------------- steps
// The stepper only shows the current step, so everything the steps share (the definition, the test inputs and the
// latest test result) lives here and not in the steps themselves.
const steps = computed((): StepperItem[] => [
    {
        value: 'general',
        slot: 'general',
        title: t('customSource.steps.general.title'),
        description: t('customSource.steps.general.description'),
        icon: 'i-lucide-globe',
    },
    {
        value: 'search',
        slot: 'search',
        title: t('customSource.steps.search.title'),
        description: t('customSource.steps.search.description'),
        icon: 'i-lucide-search',
    },
    {
        value: 'series',
        slot: 'series',
        title: t('customSource.steps.series.title'),
        description: t('customSource.steps.series.description'),
        icon: 'i-lucide-image',
    },
    {
        value: 'chapters',
        slot: 'chapters',
        title: t('customSource.steps.chapters.title'),
        description: t('customSource.steps.chapters.description'),
        icon: 'i-lucide-list-ordered',
    },
    {
        value: 'pages',
        slot: 'pages',
        title: t('customSource.steps.pages.title'),
        description: t('customSource.steps.pages.description'),
        icon: 'i-lucide-images',
    },
]);
const step = ref('general');
const stepper = useTemplateRef<{ next: () => void; prev: () => void; hasNext: boolean; hasPrev: boolean }>('stepper');

// The latest test of every step. The sidebar shows the one of the step that is open, so going from the search to the
// chapters step does not leave the search result next to the chapter selectors.
const tests = reactive<Partial<Record<CustomSourceTestStep, CustomSourceTestState>>>({});
const testStepOf: Record<string, CustomSourceTestStep | undefined> = {
    search: 'search',
    series: 'series',
    chapters: 'chapters',
    pages: 'images',
};
const currentTest = computed(() => {
    const testStep = testStepOf[step.value];
    return testStep ? tests[testStep] : undefined;
});

const { data: capabilities } = await useTranga<GetMangasCustomDownloadersCapabilitiesResponse>('/mangas/customDownloaders/capabilities', {
    key: ApiKeys.CustomDownloaderCapabilities,
});
const { data: examples } = props.showExamples
    ? await useTranga<GetMangasCustomDownloadersExamplesResponse>('/mangas/customDownloaders/examples', {
          key: ApiKeys.CustomDownloaderExamples,
      })
    : { data: ref<GetMangasCustomDownloadersExamplesResponse>() };

const flareSolverrMissing = computed(
    () => capabilities.value !== undefined && capabilities.value !== null && !capabilities.value.flareSolverrConfigured,
);

const exampleItems = computed<DropdownMenuItem[]>(() =>
    (examples.value ?? []).map((example) => ({
        label: example.name ?? t('customSource.form.exampleFallback'),
        icon: 'i-lucide-wand-sparkles',
        onSelect: () => {
            loadState({ ...normalizeCustomSource(example), id: state.value.id });
            toast.add({
                title: t('customSource.form.loadedExample', { name: example.name }),
                description: t('customSource.form.reviewHint'),
                color: 'info',
            });
        },
    })),
);

// ----------------------------------------------------------------------- format (HTML or JSON)
const isJson = computed(() => state.value.format === 'json');
const formatItems = computed(() => [
    { value: 'html', label: t('customSource.format.html.label'), description: t('customSource.format.html.description') },
    { value: 'json', label: t('customSource.format.json.label'), description: t('customSource.format.json.description') },
]);

// What differs between the formats in the texts of the steps: a selector picks an element, a path a value.
const texts = computed(() => {
    const kind = isJson.value ? 'json' : 'html';
    const text = (key: string, params: Record<string, string> = {}) => t(`customSource.texts.${kind}.${key}`, params);
    return {
        introTitle: text('introTitle'),
        intro: text('intro'),
        urlNote: isJson.value ? text('urlNote') : '',
        // Examples of what to type are code, not prose: they are not translated.
        searchUrlPlaceholder: isJson.value ? '/api/search?q={query}' : '/search?q={query}',
        searchItemLabel: text('searchItemLabel'),
        searchItem: text('searchItem'),
        searchItemPlaceholder: isJson.value ? 'data' : 'div.search-result',
        link: isJson.value ? text('link', { regex: '^(.+)$', replacement: '/series/$1' }) : undefined,
        cover: text('cover'),
        seriesCover: text('seriesCover'),
        chapterItemLabel: text('chapterItemLabel'),
        chapterItem: text('chapterItem'),
        chapterItemPlaceholder: isJson.value ? 'chapters' : 'ul.chapters > li',
        chapterNumber: text('chapterNumber', { regex: '^(\\d+(?:\\.\\d*[1-9])?)' }),
        imageLabel: text('imageLabel'),
        image: text('image'),
        imagePlaceholder: isJson.value ? 'pages' : 'div.reader img',
        pageLinks: text('pageLinks'),
    };
});

// The description of the search URL: what {query} is, and for an API what address to enter.
const searchUrlDescription = computed(() =>
    [t('customSource.search.urlDescription', { query: '{query}' }), texts.value.urlNote].filter(Boolean).join(' '),
);

// The prompt lives next to the examples in the repository, so it can be improved without a release of the frontend.
const agentPromptUrl = 'https://github.com/C9Glax/tranga/blob/main/Extensions/Custom/Examples/agent-prompt.md';

// Only codes like en, en-us or pt-BR can be added as language: anything else is dropped with a message.
const languageError = ref<string>();
const languages = computed({
    get: () => state.value.languages,
    set: (tags: string[]) => {
        const rejected = tags.filter((tag) => !isLanguageCode(tag));
        languageError.value = rejected.length
            ? t('customSource.errors.notALanguageCode', { rejected: `"${rejected.join('", "')}"` })
            : undefined;
        state.value.languages = tags.filter((tag) => isLanguageCode(tag)).map((tag) => tag.trim());
    },
});

// What the tests try (and the URL the series URL pattern is tried on). A test result can fill in the one of the next step.
const testUrls = reactive({ pattern: '', search: '', series: '', chapters: '', images: '' });
const patternResult = computed(() => testSeriesPattern(state.value.seriesUrlPattern, testUrls.pattern));

const useSeriesUrl = (url: string) => {
    testUrls.series = url;
    testUrls.chapters = url;
    testUrls.pattern = url;
    toast.add({
        title: t('customSource.form.filledSeriesUrl'),
        description: t('customSource.form.filledSeriesUrlDescription'),
        color: 'info',
    });
};
// A row of a test result was picked for the next step: a search result is a series (chapters test), a chapter leads to
// its pages (pages test).
const pickFromResults = (from: CustomSourceTestStep, url: string) => {
    if (from === 'search') return useSeriesUrl(url);
    testUrls.images = url;
    toast.add({
        title: t('customSource.form.filledChapterUrl'),
        description: t('customSource.form.filledChapterUrlDescription'),
        color: 'info',
    });
};

// The header rows are edited as a list; keep the definition (used by the test panels and on save) in sync.
const syncHeaders = () => {
    state.value.headers = Object.fromEntries(
        headerRows.value.filter((row) => row.name.trim() !== '').map((row) => [row.name.trim(), row.value]),
    );
};
watch(headerRows, syncHeaders, { deep: true });

// The source can only be created / saved on the last step, after all of them have been seen.
const isLastStep = computed(() => step.value === steps.value[steps.value.length - 1]?.value);
const isGeneralStep = computed(() => step.value === steps.value[0]?.value);

// The pattern test in the sidebar starts at the height of the pattern field. The sidebar only sits next to the steps
// from the `lg` breakpoint on (below that it is stacked underneath), and the height changes with the content above the
// field (an error line, an example being loaded, the window width), so it is measured again when the layout changes.
const sidebar = useTemplateRef<HTMLElement>('sidebar');
const patternInput = useTemplateRef<HTMLElement>('patternInput');
const patternOffset = ref(0);
const measurePatternOffset = () => {
    const sideBySide = window.matchMedia('(min-width: 1024px)').matches;
    if (!sideBySide || !sidebar.value || !patternInput.value) {
        patternOffset.value = 0;
        return;
    }
    // The sidebar's own top edge does not move with its padding, so the difference is the padding it needs.
    const offset = Math.round(patternInput.value.getBoundingClientRect().top - sidebar.value.getBoundingClientRect().top);
    patternOffset.value = Math.max(0, offset);
};
useResizeObserver(() => sidebar.value?.parentElement, measurePatternOffset);
onMounted(() => measurePatternOffset());
watch(step, () => nextTick(measurePatternOffset));
watch(isJson, () => nextTick(measurePatternOffset)); // the texts above the pattern field differ in height

const submit = () => {
    // Pressing Enter in a field of an earlier step must not save a half finished source.
    if (!isLastStep.value) return;
    syncHeaders();
    emit('submit', state.value);
};

// ----------------------------------------------------------------------- JSON show / import
const jsonOpen = ref(false);
const json = ref('');

const openJson = () => {
    syncHeaders();
    json.value = customSourceToJson(state.value);
    jsonOpen.value = true;
};

const copyJson = async () => {
    try {
        await navigator.clipboard.writeText(json.value);
        toast.add({ title: t('customSource.form.copied'), color: 'success' });
    } catch {
        toast.add({ title: t('customSource.form.copyFailed'), description: t('customSource.form.copyFailedDescription'), color: 'error' });
    }
};

const importOpen = ref(false);
const importText = ref('');
const importError = ref<string>();

const openImport = () => {
    importText.value = '';
    importError.value = undefined;
    importOpen.value = true;
};

const importJson = () => {
    const { source, error } = customSourceFromJson(importText.value);
    if (!source) {
        importError.value = error;
        return;
    }
    loadState({ ...source, id: state.value.id });
    importOpen.value = false;
    toast.add({ title: t('customSource.form.imported'), description: t('customSource.form.reviewHint'), color: 'success' });
};
</script>
