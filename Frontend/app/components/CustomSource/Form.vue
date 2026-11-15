<template>
    <div class="flex flex-col gap-4">
        <UAlert
            color="neutral"
            variant="subtle"
            icon="i-lucide-info"
            title="Describe the website with selectors"
            description="Open the website, right-click an element, choose Inspect, then right-click it in the inspector and pick Copy → Copy selector (CSS) or Copy XPath. Selectors of fields are relative to their item (the element repeated for every search result / chapter). Test each step on the real website before saving." />

        <div class="flex flex-row flex-wrap gap-2">
            <UDropdownMenu v-if="examples?.length" :items="exampleItems">
                <UButton
                    label="Start from an example"
                    icon="i-lucide-wand-sparkles"
                    color="neutral"
                    variant="subtle"
                    trailing-icon="i-lucide-chevron-down" />
            </UDropdownMenu>
            <UButton label="JSON" icon="i-lucide-braces" color="neutral" variant="subtle" @click="openJson" />
        </div>

        <div class="grid grid-cols-1 gap-4 lg:grid-cols-3">
            <UForm :state="state" class="flex min-w-0 flex-col gap-4 lg:col-span-2" @submit="submit">
                <UStepper ref="stepper" v-model="step" :items="steps" :linear="false" class="w-full">
                    <template #general>
                        <div class="flex flex-col gap-4 pt-4">
                            <UFormField label="Name" name="name" required>
                                <UInput v-model="state.name" placeholder="My Manga Site" class="w-full" />
                            </UFormField>
                            <UFormField
                                label="Base URL"
                                name="baseUrl"
                                required
                                :description="`Root of the website. Relative links are resolved against it. A trailing / is removed.${privateHint}`">
                                <UInput
                                    v-model="state.baseUrl"
                                    placeholder="https://example.com"
                                    class="w-full"
                                    @blur="state.baseUrl = normalizeBaseUrl(state.baseUrl)" />
                            </UFormField>
                            <UFormField label="Icon URL" name="iconUrl" description="Optional. Defaults to the website's favicon.">
                                <UInput v-model="state.iconUrl" placeholder="https://example.com/icon.png" class="w-full" />
                            </UFormField>
                            <UFormField
                                label="Languages"
                                name="languages"
                                required
                                :error="languageError"
                                description="Languages the website publishes in, as codes like en, en-us or pt-BR. Press Enter to add one.">
                                <UInputTags v-model="languages" placeholder="en" class="w-full" />
                            </UFormField>
                            <UFormField
                                label="Series URL pattern"
                                name="seriesUrlPattern"
                                description="Optional regular expression a series URL has to match. Named groups like (?<id>[^/]+) can be used as {id} in the chapter list URL. Try it on a URL on the right.">
                                <div class="flex flex-col gap-2">
                                    <div class="grid gap-2 md:grid-cols-2">
                                        <UInput
                                            v-model="state.seriesUrlPattern"
                                            placeholder="^https://example\.com/series/(?<id>[^/]+)"
                                            :ui="{ base: 'font-mono' }"
                                            class="w-full" />
                                        <UInput
                                            v-model="testUrls.pattern"
                                            placeholder="A series URL to try the pattern on"
                                            class="w-full" />
                                    </div>
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
                                                    {{ placeholder(group.name) }} = {{ group.value || '(empty)' }}
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
                                    :description="
                                        flareSolverrMissing
                                            ? 'No FlareSolverr instance is configured (FLARESOLVERR_URL).'
                                            : 'Route requests through FlareSolverr, for Cloudflare protected sites.'
                                    ">
                                    <USwitch v-model="state.useFlareSolverr" :disabled="flareSolverrMissing" label="Use FlareSolverr" />
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
                                description="{query} is replaced with the URL-encoded search text. Relative to the base URL.">
                                <UInput
                                    v-model="state.search.urlTemplate"
                                    placeholder="/search?q={query}"
                                    :ui="{ base: 'font-mono' }"
                                    class="w-full" />
                            </UFormField>
                            <UFormField
                                label="Result item selector"
                                name="search.itemSelector"
                                required
                                description="One element per search result.">
                                <UInput
                                    v-model="state.search.itemSelector"
                                    placeholder="div.search-result"
                                    :ui="{ base: 'font-mono' }"
                                    class="w-full" />
                            </UFormField>
                            <CustomSourceFieldInput v-model="state.search.title" label="Title" required />
                            <CustomSourceFieldInput
                                v-model="state.search.url"
                                label="Series link"
                                required
                                attribute-placeholder="Attribute, e.g. href"
                                description="Relative links are resolved." />
                            <CustomSourceFieldInput
                                v-model="state.search.cover"
                                label="Cover image"
                                required
                                attribute-placeholder="Attribute, e.g. src or data-src,src"
                                description="Several attributes can be listed, the first one that has a value is used." />
                            <CustomSourceTestPanel
                                v-model:input="testUrls.search"
                                v-model:test="test"
                                :definition="state"
                                step="search"
                                input-label="Search text"
                                input-placeholder="one piece" />
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
                                label="Chapter item selector"
                                name="chapters.itemSelector"
                                required
                                description="One element per chapter.">
                                <UInput
                                    v-model="state.chapters.itemSelector"
                                    placeholder="ul.chapters > li"
                                    :ui="{ base: 'font-mono' }"
                                    class="w-full" />
                            </UFormField>
                            <CustomSourceFieldInput
                                v-model="state.chapters.url"
                                label="Chapter link"
                                required
                                attribute-placeholder="Attribute, e.g. href" />
                            <CustomSourceFieldInput
                                v-model="state.chapters.number"
                                label="Chapter number"
                                description="Leave empty to search the whole item. The first number found is used (Chapter 12.5 → 12.5); chapters without a number are skipped. Use a regex to be more specific." />
                            <CustomSourceFieldInput v-model="state.chapters.title" label="Chapter title" description="Optional." />
                            <CustomSourceFieldInput
                                v-model="state.chapters.date"
                                label="Release date"
                                description="Optional."
                                attribute-placeholder="Attribute, e.g. datetime" />
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
                                label="Next page link"
                                description="Optional. When the chapter list is split over several pages, the link to the next one (followed up to 50 times)."
                                attribute-placeholder="Attribute, e.g. href" />
                            <CustomSourceTestPanel
                                v-model:input="testUrls.chapters"
                                v-model:test="test"
                                :definition="state"
                                step="chapters"
                                input-label="Series URL"
                                input-placeholder="https://example.com/series/1"
                                help="A series page of the website. Use the button in the search test results to fill it in." />
                        </div>
                    </template>

                    <template #pages>
                        <div class="flex flex-col gap-4 pt-4">
                            <p class="text-sm text-muted">Finds the page images of a chapter.</p>
                            <UFormField
                                label="Pages URL"
                                name="images.urlTemplate"
                                required
                                description="{url} is the chapter link. Some sites load the images from a separate URL, e.g. {url}/images">
                                <UInput v-model="state.images.urlTemplate" placeholder="{url}" :ui="{ base: 'font-mono' }" class="w-full" />
                            </UFormField>
                            <CustomSourceFieldInput
                                v-model="state.images.pageSelector"
                                label="Page links"
                                description="Optional. Only for sites that show one image per web page: the links (or the options of a page dropdown) to every page of the chapter, in reading order. Every page is then opened to find its image."
                                selector-placeholder="e.g. select.pages option"
                                attribute-placeholder="Attribute (default href,value)" />
                            <UFormField
                                label="Image selector"
                                name="images.imageSelector"
                                required
                                description="One element per page image, in reading order. With page links: the image on each of those pages.">
                                <UInput
                                    v-model="state.images.imageSelector"
                                    placeholder="div.reader img"
                                    :ui="{ base: 'font-mono' }"
                                    class="w-full" />
                            </UFormField>
                            <UFormField
                                label="Image attribute"
                                name="images.attribute"
                                description="Defaults to data-src,data-lazy-src,src (the first one that has a value).">
                                <UInput
                                    v-model="state.images.attribute"
                                    placeholder="data-src,src"
                                    :ui="{ base: 'font-mono' }"
                                    class="w-full" />
                            </UFormField>
                            <CustomSourceTestPanel
                                v-model:input="testUrls.images"
                                v-model:test="test"
                                :definition="state"
                                step="images"
                                input-label="Chapter URL"
                                input-placeholder="https://example.com/chapter/1"
                                help="Lists the image URLs that would be downloaded; nothing is downloaded. Use the button in the chapters test results to fill it in." />
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
                        <UButton type="submit" :label="submitLabel" icon="i-lucide-save" :loading="saving" />
                    </div>
                </div>
            </UForm>

            <CustomSourceTestResults :test="test" class="lg:col-span-1" @pick="pickFromResults" />
        </div>

        <UModal
            v-model:open="jsonOpen"
            title="Source as JSON"
            description="Copy this to share the source, or paste someone else's definition and load it into the form.">
            <template #body>
                <div class="flex flex-col gap-3">
                    <UTextarea v-model="json" :rows="18" :ui="{ base: 'font-mono text-xs' }" class="w-full" />
                    <UAlert v-if="jsonError" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="jsonError" />
                </div>
            </template>
            <template #footer>
                <div class="flex w-full flex-row justify-end gap-2">
                    <UButton label="Copy" icon="i-lucide-copy" color="neutral" variant="subtle" @click="copyJson" />
                    <UButton label="Load into form" icon="i-lucide-download" @click="loadJson" />
                </div>
            </template>
        </UModal>
    </div>
</template>

<script setup lang="ts">
import type { DropdownMenuItem } from '@nuxt/ui/components/DropdownMenu.vue';
import type { StepperItem } from '@nuxt/ui/components/Stepper.vue';
import type { GetMangasCustomDownloadersCapabilitiesResponse, GetMangasCustomDownloadersExamplesResponse } from '~/api/tranga';
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

const test = ref<CustomSourceTestState>();

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
const privateHint = computed(() =>
    capabilities.value && !capabilities.value.allowPrivateHosts
        ? ' Only public websites can be used: addresses on your own network are blocked (ALLOW_PRIVATE_SOURCE_HOSTS).'
        : '',
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
const placeholder = (group: string) => `{${group}}`; // how the group is written in the chapter list URL

const useSeriesUrl = (url: string) => {
    testUrls.chapters = url;
    testUrls.pattern = url;
    toast.add({
        title: 'Filled in the series URL.',
        description: 'It is used in the pattern preview and the chapters test.',
        color: 'info',
    });
};
const useChapterUrl = (url: string) => {
    testUrls.images = url;
    toast.add({ title: 'Filled in the chapter URL.', description: 'It is used in the pages test.', color: 'info' });
};
const pickFromResults = (from: CustomSourceTestStep, url: string) => (from === 'search' ? useSeriesUrl(url) : useChapterUrl(url));

// The header rows are edited as a list; keep the definition (used by the test panels and on save) in sync.
const syncHeaders = () => {
    state.value.headers = Object.fromEntries(
        headerRows.value.filter((row) => row.name.trim() !== '').map((row) => [row.name.trim(), row.value]),
    );
};
watch(headerRows, syncHeaders, { deep: true });

const submit = () => {
    syncHeaders();
    emit('submit', state.value);
};

// ----------------------------------------------------------------------- JSON import / export
const jsonOpen = ref(false);
const json = ref('');
const jsonError = ref<string>();

const openJson = () => {
    syncHeaders();
    json.value = customSourceToJson(state.value);
    jsonError.value = undefined;
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

const loadJson = () => {
    const { source, error } = customSourceFromJson(json.value);
    if (!source) {
        jsonError.value = error;
        return;
    }
    loadState({ ...source, id: state.value.id });
    jsonOpen.value = false;
    toast.add({ title: 'Loaded the definition into the form.', description: 'Review it before saving.', color: 'success' });
};
</script>
