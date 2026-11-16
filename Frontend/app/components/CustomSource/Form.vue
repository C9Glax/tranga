<template>
    <div class="flex flex-col gap-4">
        <div class="flex flex-row flex-wrap gap-2">
            <UDropdownMenu v-if="examples?.length" :items="exampleItems">
                <UButton
                    label="Start from an example"
                    icon="i-lucide-wand-sparkles"
                    color="neutral"
                    variant="subtle"
                    trailing-icon="i-lucide-chevron-down" />
            </UDropdownMenu>
            <UButton label="Show JSON" icon="i-lucide-braces" color="neutral" variant="subtle" @click="openJson" />
            <UButton label="Import JSON" icon="i-lucide-file-input" color="neutral" variant="subtle" @click="openImport" />
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
                                :description="`${texts.intro} Test each step on the real website before saving. Rather have an AI agent do the looking? Give it the prompt linked here, then paste its answer with Import JSON.`"
                                :actions="[
                                    {
                                        label: 'Prompt for AI agents',
                                        icon: 'i-lucide-bot',
                                        to: agentPromptUrl,
                                        target: '_blank',
                                        color: 'neutral',
                                        variant: 'outline',
                                    },
                                ]" />
                            <UFormField label="Name" name="name" required>
                                <UInput v-model="state.name" placeholder="My Manga Site" class="w-full" />
                            </UFormField>
                            <UFormField label="Base URL" name="baseUrl" required>
                                <UInput
                                    v-model="state.baseUrl"
                                    placeholder="https://example.com"
                                    class="w-full"
                                    @blur="state.baseUrl = normalizeBaseUrl(state.baseUrl)" />
                            </UFormField>
                            <UFormField
                                label="Response format"
                                name="format"
                                required
                                description="What the website answers with. It applies to every step and decides how the answers are read.">
                                <URadioGroup
                                    v-model="state.format"
                                    :items="formatItems"
                                    variant="card"
                                    :ui="{ fieldset: 'grid grid-cols-1 gap-2 sm:grid-cols-2' }"
                                    class="w-full" />
                            </UFormField>
                            <UFormField label="Icon URL" name="iconUrl" description="Optional. Defaults to the website's favicon.">
                                <UInput v-model="state.iconUrl" placeholder="https://example.com/icon.png" class="w-full" />
                            </UFormField>
                            <UFormField
                                label="Languages"
                                name="languages"
                                required
                                :error="languageError"
                                description="Languages the website publishes in, as codes like en, en-us or pt-BR.">
                                <UInputTags v-model="languages" placeholder="en" class="w-full" />
                            </UFormField>
                            <UFormField
                                label="Series URL pattern"
                                name="seriesUrlPattern"
                                description="Optional regular expression a series URL has to match. Named groups like (?<id>[^/]+) can be used as {id} in the chapter list URL. Try it on a URL on the right.">
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
                                    label="Requests per second"
                                    name="requestsPerSecond"
                                    description="Limit for the website itself (0.1 - 20). 0.5 = one request every two seconds.">
                                    <UInputNumber
                                        v-model="state.requestsPerSecond"
                                        :min="0.1"
                                        :max="20"
                                        :step="0.1"
                                        :format-options="{ maximumFractionDigits: 2 }" />
                                </UFormField>
                                <UFormField
                                    label="FlareSolverr"
                                    name="useFlareSolverr"
                                    description="Route requests through FlareSolverr, for Cloudflare protected sites.">
                                    <!-- Only explains itself while the switch is disabled. The wrapper keeps hovering working on a disabled switch. -->
                                    <UTooltip
                                        text="No FlareSolverr instance is configured (FLARESOLVERR_URL)."
                                        :disabled="!flareSolverrMissing">
                                        <span class="inline-flex">
                                            <USwitch
                                                v-model="state.useFlareSolverr"
                                                :disabled="flareSolverrMissing"
                                                label="Use FlareSolverr" />
                                        </span>
                                    </UTooltip>
                                </UFormField>
                            </div>
                            <UFormField
                                label="Request headers"
                                name="headers"
                                description="Optional, sent with every request, e.g. Cookie or User-Agent. Requests look like a regular browser unless you set a User-Agent.">
                                <div class="flex flex-col gap-2">
                                    <div v-for="(row, index) in headerRows" :key="index" class="flex flex-row gap-2">
                                        <UInput v-model="row.name" placeholder="Header name" class="w-56" />
                                        <UInput v-model="row.value" placeholder="Value" class="grow" />
                                        <UButton
                                            icon="i-lucide-trash-2"
                                            color="neutral"
                                            variant="ghost"
                                            aria-label="Remove header"
                                            @click="headerRows.splice(index, 1)" />
                                    </div>
                                    <div>
                                        <UButton
                                            label="Add header"
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
                            <p class="text-sm text-muted">Finds the series for a search text.</p>
                            <UFormField
                                label="Search URL"
                                name="search.urlTemplate"
                                required
                                :description="`{query} is replaced with the URL-encoded search text. Relative to the base URL.${texts.urlNote}`">
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
                                label="Title"
                                required
                                json-placeholder="e.g. title" />
                            <CustomSourceFieldInput
                                v-model="state.search.url"
                                :format="state.format"
                                label="Series link"
                                required
                                :description="texts.link"
                                attribute-placeholder="e.g. href"
                                json-placeholder="e.g. url or slug" />
                            <CustomSourceFieldInput
                                v-model="state.search.cover"
                                :format="state.format"
                                label="Cover image"
                                required
                                attribute-placeholder="e.g. data-src,src"
                                json-placeholder="e.g. cover"
                                :description="texts.cover" />
                        </div>
                    </template>

                    <template #chapters>
                        <div class="flex flex-col gap-4 pt-4">
                            <p class="text-sm text-muted">Lists the chapters of a series.</p>
                            <UFormField
                                label="Chapter list URL"
                                name="chapters.urlTemplate"
                                required
                                description="{url} is the series link. Named groups of the series URL pattern can be used too, e.g. https://example.com/series/{id}/chapters">
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
                                label="Chapter link"
                                required
                                :description="texts.link"
                                attribute-placeholder="e.g. href"
                                json-placeholder="e.g. url or id" />
                            <CustomSourceFieldInput
                                v-model="state.chapters.number"
                                :format="state.format"
                                label="Chapter number"
                                :required="isJson"
                                :description="texts.chapterNumber"
                                json-placeholder="e.g. chapter_number" />
                            <CustomSourceFieldInput
                                v-model="state.chapters.title"
                                :format="state.format"
                                label="Chapter title"
                                description="Optional."
                                json-placeholder="e.g. title" />
                            <CustomSourceFieldInput
                                v-model="state.chapters.date"
                                :format="state.format"
                                label="Release date"
                                description="Optional."
                                attribute-placeholder="e.g. datetime"
                                json-placeholder="e.g. published_at" />
                            <UFormField
                                label="Date format"
                                name="chapters.dateFormat"
                                description="Optional .NET format like dd/MM/yyyy. ISO dates and common formats work without it.">
                                <UInput
                                    v-model="state.chapters.dateFormat"
                                    placeholder="yyyy-MM-dd"
                                    :ui="{ base: 'font-mono' }"
                                    class="w-full" />
                            </UFormField>
                            <CustomSourceFieldInput
                                v-model="state.chapters.nextPage"
                                :format="state.format"
                                label="Next page link"
                                description="Optional. When the chapter list is split over several pages, the link to the next one (followed up to 50 times)."
                                attribute-placeholder="e.g. href"
                                json-placeholder="e.g. next" />
                        </div>
                    </template>

                    <template #pages>
                        <div class="flex flex-col gap-4 pt-4">
                            <p class="text-sm text-muted">Finds the page images of a chapter.</p>
                            <UFormField
                                label="Pages URL"
                                name="images.urlTemplate"
                                required
                                description="{url} is the chapter link. Some sites load the images from a separate URL, e.g. {url}/images. Named groups of the chapter URL pattern can be used too, e.g. /api/chapter/{id}">
                                <UInput v-model="state.images.urlTemplate" placeholder="{url}" :ui="{ base: 'font-mono' }" class="w-full" />
                            </UFormField>
                            <UFormField
                                label="Chapter URL pattern"
                                name="images.urlPattern"
                                description="Optional regular expression the chapter link is matched against. Named groups like (?<id>\d+) can be used as {id} in the pages URL, for sites that load the pages from an address that only contains a part of the chapter link.">
                                <UInput
                                    v-model="state.images.urlPattern"
                                    placeholder="/chapter/(?<id>\d+)"
                                    :ui="{ base: 'font-mono' }"
                                    class="w-full" />
                            </UFormField>
                            <CustomSourceFieldInput
                                v-model="state.images.pageSelector"
                                :format="state.format"
                                json-placeholder="e.g. pages[*].url"
                                label="Page links"
                                :description="texts.pageLinks"
                                selector-placeholder="e.g. select.pages option"
                                attribute-placeholder="default: href,value"
                                attribute-hint="Leave empty to use href for links and value for the options of a page dropdown." />
                            <UFormField :label="texts.imageLabel" name="images.imageSelector" required :description="texts.image">
                                <UInput
                                    v-model="state.images.imageSelector"
                                    :placeholder="texts.imagePlaceholder"
                                    :ui="{ base: 'font-mono' }"
                                    class="w-full" />
                            </UFormField>
                            <UFormField
                                v-if="!isJson"
                                label="Image attribute"
                                name="images.attribute"
                                description="Defaults to data-src,data-lazy-src,src (the first one that has a value).">
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
                    title="The source could not be saved">
                    <template #description>
                        <ul class="list-disc pl-4">
                            <li v-for="error in errors" :key="error">{{ error }}</li>
                        </ul>
                    </template>
                </UAlert>

                <div class="flex flex-row flex-wrap items-center justify-between gap-2">
                    <UButton
                        label="Back"
                        icon="i-lucide-arrow-left"
                        color="neutral"
                        variant="outline"
                        :disabled="!stepper?.hasPrev"
                        @click="stepper?.prev()" />
                    <div class="flex flex-row flex-wrap justify-end gap-2">
                        <UButton label="Cancel" color="neutral" variant="ghost" to="/settings/sources" />
                        <UButton
                            label="Next"
                            trailing-icon="i-lucide-arrow-right"
                            color="neutral"
                            variant="outline"
                            :disabled="!stepper?.hasNext"
                            @click="stepper?.next()" />
                        <UButton v-if="isLastStep" type="submit" :label="submitLabel" icon="i-lucide-save" :loading="saving" />
                    </div>
                </div>
            </UForm>

            <!-- The sidebar: on the General step the test of the series URL pattern (level with the pattern field), on the
                 other steps the latest test result of that step. -->
            <div ref="sidebar" class="min-w-0 lg:col-span-1" :style="{ paddingTop: `${isGeneralStep ? patternOffset : 0}px` }">
                <div v-if="isGeneralStep" class="flex flex-col gap-2">
                    <UInput
                        v-model="testUrls.pattern"
                        aria-label="A series URL to try the pattern on"
                        placeholder="A series URL to try the pattern on"
                        class="w-full" />
                    <div v-if="patternResult.state !== 'empty'" class="flex flex-row flex-wrap items-center gap-2 text-sm">
                        <template v-if="patternResult.state === 'invalid'">
                            <UIcon name="i-lucide-circle-alert" class="text-error" />
                            <span class="text-error">{{ patternResult.message }}</span>
                        </template>
                        <template v-else-if="patternResult.state === 'no-match'">
                            <UIcon name="i-lucide-x" class="text-warning" />
                            <span class="text-warning">The pattern does not match this URL.</span>
                        </template>
                        <template v-else>
                            <UIcon name="i-lucide-check" class="text-success" />
                            <span class="text-success">Matches.</span>
                            <template v-if="patternResult.groups.length">
                                <UBadge
                                    v-for="group in patternResult.groups"
                                    :key="group.name"
                                    color="neutral"
                                    variant="subtle"
                                    class="font-mono">
                                    <!-- shown the way the group is written in the chapter list URL: {name} -->
                                    {{ '{' + group.name + '}' }} = {{ group.value || '(empty)' }}
                                </UBadge>
                            </template>
                            <span v-else class="text-muted">No named groups.</span>
                            <UButton
                                label="Use for the chapters test"
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
                        input-label="Search text"
                        input-placeholder="one piece" />
                    <CustomSourceTestPanel
                        v-else-if="step === 'chapters'"
                        v-model:input="testUrls.chapters"
                        v-model:test="tests.chapters"
                        :definition="state"
                        step="chapters"
                        input-label="Series URL"
                        input-placeholder="https://example.com/series/1"
                        help="A series page of the website. Use the button in the search test results to fill it in." />
                    <CustomSourceTestPanel
                        v-else-if="step === 'pages'"
                        v-model:input="testUrls.images"
                        v-model:test="tests.images"
                        :definition="state"
                        step="images"
                        input-label="Chapter URL"
                        input-placeholder="https://example.com/chapter/1"
                        help="Lists the image URLs that would be downloaded; nothing is downloaded. Use the button in the chapters test results to fill it in." />
                    <CustomSourceTestResults :test="currentTest" @pick="pickFromResults" />
                </div>
            </div>
        </div>

        <UModal
            v-model:open="jsonOpen"
            title="Source as JSON"
            description="The definition as it is in the form now. Copy it to share the source.">
            <template #body>
                <UTextarea :model-value="json" readonly :rows="18" :ui="{ base: 'font-mono text-xs' }" class="w-full" />
            </template>
            <template #footer>
                <div class="flex w-full flex-row justify-end gap-2">
                    <UButton label="Close" color="neutral" variant="ghost" @click="jsonOpen = false" />
                    <UButton label="Copy" icon="i-lucide-copy" @click="copyJson" />
                </div>
            </template>
        </UModal>

        <UModal
            v-model:open="importOpen"
            title="Import JSON"
            description="Paste someone else's definition. It replaces what is in the form; nothing is saved until you create the source.">
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
                    <UButton label="Cancel" color="neutral" variant="ghost" @click="importOpen = false" />
                    <UButton label="Import" icon="i-lucide-file-input" :disabled="!importText.trim()" @click="importJson" />
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
    { submitLabel: 'Save', saving: false, errors: () => [], showExamples: false },
);
const emit = defineEmits<{ submit: [definition: CustomSourceDefinition] }>();

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
const steps: StepperItem[] = [
    { value: 'general', slot: 'general', title: 'General', description: 'The website', icon: 'i-lucide-globe' },
    { value: 'search', slot: 'search', title: 'Search', description: 'Find series', icon: 'i-lucide-search' },
    { value: 'chapters', slot: 'chapters', title: 'Chapters', description: 'List chapters', icon: 'i-lucide-list-ordered' },
    { value: 'pages', slot: 'pages', title: 'Pages', description: 'Find images', icon: 'i-lucide-images' },
];
const step = ref('general');
const stepper = useTemplateRef<{ next: () => void; prev: () => void; hasNext: boolean; hasPrev: boolean }>('stepper');

// The latest test of every step. The sidebar shows the one of the step that is open, so going from the search to the
// chapters step does not leave the search result next to the chapter selectors.
const tests = reactive<Partial<Record<CustomSourceTestStep, CustomSourceTestState>>>({});
const testStepOf: Record<string, CustomSourceTestStep | undefined> = { search: 'search', chapters: 'chapters', pages: 'images' };
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
        label: example.name ?? 'Example',
        icon: 'i-lucide-wand-sparkles',
        onSelect: () => {
            loadState({ ...normalizeCustomSource(example), id: state.value.id });
            toast.add({
                title: `Loaded example "${example.name}".`,
                description: 'Review it and use the test buttons before saving.',
                color: 'info',
            });
        },
    })),
);

// ----------------------------------------------------------------------- format (HTML or JSON)
const isJson = computed(() => state.value.format === 'json');
const formatItems = [
    { value: 'html', label: 'HTML', description: 'Web pages, read with CSS selectors or XPath. What most websites need.' },
    {
        value: 'json',
        label: 'JSON',
        description: 'An API, read with paths. For websites whose pages are empty until JavaScript loads the content.',
    },
];

// What differs between the formats in the texts of the steps: a selector picks an element, a path a value.
const texts = computed(() =>
    isJson.value
        ? {
              introTitle: "Describe the website's API with paths",
              intro: "The pages of this website are built by JavaScript, so there is nothing in them to read: the content comes from an API. Open the website with the browser's developer tools on the Network tab (filter Fetch/XHR), search and open a chapter, and look for the requests that answer with JSON. Their addresses go into the URL fields; paths such as data, manga.title or pages[0] lead to the values in the answer. Paths of fields are relative to their item (one search result / chapter).",
              urlNote: ' The address of the API that answers with the results, not the search page.',
              searchUrlPlaceholder: '/api/search?q={query}',
              searchItemLabel: 'Result items path',
              searchItem:
                  'The path to the list of search results in the answer, e.g. data or results.items. The title, link and cover below are looked up inside each result, so their paths are relative to it.',
              searchItemPlaceholder: 'data',
              link: 'An API often only has an id or slug: build the link with a regex and a replacement, e.g. ^(.+)$ and /series/$1.',
              cover: 'Several paths can be listed, the first one that has a value is used.',
              chapterItemLabel: 'Chapter items path',
              chapterItem: 'The path to the list of chapters in the answer.',
              chapterItemPlaceholder: 'chapters',
              chapterNumber:
                  'The first number found is used (Chapter 12.5 → 12.5); chapters without a number are skipped. An API often pads numbers (12.5000): cut that off with a regex such as ^(\\d+(?:\\.\\d*[1-9])?)',
              imageLabel: 'Image URLs path',
              image: 'The path to the image URLs of the chapter, in reading order, e.g. pages or pages[*].url. With page links: the image in the answer of each of those links.',
              imagePlaceholder: 'pages',
              pageLinks:
                  'Optional. Only for an API that has to be asked for every page of a chapter on its own: the path to the addresses of those requests, in reading order. Each of them is then requested to find its image. Leave it empty when one answer lists all the images.',
          }
        : {
              introTitle: 'Describe the website with selectors',
              intro: 'Open the website, right-click an element, choose Inspect, then right-click it in the inspector and pick Copy → Copy selector (CSS) or Copy XPath. Selectors of fields are relative to their item (the element repeated for every search result / chapter).',
              urlNote: '',
              searchUrlPlaceholder: '/search?q={query}',
              searchItemLabel: 'Result item selector',
              searchItem:
                  'One element per search result: the wrapper around the whole result. The title, link and cover below are looked up inside this element, so their selectors are relative to it.',
              searchItemPlaceholder: 'div.search-result',
              link: undefined,
              cover: 'Several attributes can be listed, the first one that has a value is used.',
              chapterItemLabel: 'Chapter item selector',
              chapterItem: 'One element per chapter.',
              chapterItemPlaceholder: 'ul.chapters > li',
              chapterNumber:
                  'Leave empty to search the whole item. The first number found is used (Chapter 12.5 → 12.5); chapters without a number are skipped. Use a regex to be more specific.',
              imageLabel: 'Image selector',
              image: 'One element per page image, in reading order. With page links: the image on each of those pages.',
              imagePlaceholder: 'div.reader img',
              pageLinks:
                  'Optional. Only for sites that show one image per web page: the links (or the options of a page dropdown) to every page of the chapter, in reading order. Every page is then opened to find its image.',
          },
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
            ? `"${rejected.join('", "')}" is not a language code. Use codes like en, en-us or pt-BR.`
            : undefined;
        state.value.languages = tags.filter((tag) => isLanguageCode(tag)).map((tag) => tag.trim());
    },
});

// What the tests try (and the URL the series URL pattern is tried on). A test result can fill in the one of the next step.
const testUrls = reactive({ pattern: '', search: '', chapters: '', images: '' });
const patternResult = computed(() => testSeriesPattern(state.value.seriesUrlPattern, testUrls.pattern));

const useSeriesUrl = (url: string) => {
    testUrls.chapters = url;
    testUrls.pattern = url;
    toast.add({
        title: 'Filled in the series URL.',
        description: 'It is used in the pattern preview and the chapters test.',
        color: 'info',
    });
};
// A row of a test result was picked for the next step: a search result is a series (chapters test), a chapter leads to
// its pages (pages test).
const pickFromResults = (from: CustomSourceTestStep, url: string) => {
    if (from === 'search') return useSeriesUrl(url);
    testUrls.images = url;
    toast.add({ title: 'Filled in the chapter URL.', description: 'It is used in the pages test.', color: 'info' });
};

// The header rows are edited as a list; keep the definition (used by the test panels and on save) in sync.
const syncHeaders = () => {
    state.value.headers = Object.fromEntries(
        headerRows.value.filter((row) => row.name.trim() !== '').map((row) => [row.name.trim(), row.value]),
    );
};
watch(headerRows, syncHeaders, { deep: true });

// The source can only be created / saved on the last step, after all of them have been seen.
const isLastStep = computed(() => step.value === steps[steps.length - 1]?.value);
const isGeneralStep = computed(() => step.value === steps[0]?.value);

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
        toast.add({ title: 'Copied to the clipboard.', color: 'success' });
    } catch {
        toast.add({ title: 'Could not copy.', description: 'Select the text and copy it manually.', color: 'error' });
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
    toast.add({
        title: 'Imported the definition into the form.',
        description: 'Review it and use the test buttons before saving.',
        color: 'success',
    });
};
</script>
