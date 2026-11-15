<template>
    <div class="mx-auto flex max-w-[1500px] flex-col gap-10 p-8">
        <header class="flex flex-col gap-1">
            <h1 class="text-2xl font-semibold">Palette: what a Nuxt UI 4.12 upgrade lets us unify</h1>
            <p class="text-sm text-muted">
                Every tile is rendered with real components, using content from our own pages. Each caption says what it would replace.
            </p>
        </header>

        <!-- ------------------------------------------------------------------------------------------ testing sidebar -->
        <section id="sidebar" class="flex flex-col gap-4">
            <div>
                <h2 class="text-lg font-semibold">1. The testing sidebar, as cards</h2>
                <p class="text-sm text-muted">
                    Replaces: a bordered div around the test input, one card for the results, and result items drawn from divs. Four states
                    of the same sidebar, each with the input card on top.
                </p>
            </div>
            <div class="grid grid-cols-1 gap-6 md:grid-cols-2 xl:grid-cols-4">
                <!-- idle -->
                <div class="flex flex-col gap-4">
                    <p class="text-xs font-medium tracking-wide text-dimmed uppercase">Idle</p>
                    <UCard variant="outline" :ui="{ body: 'p-3 sm:p-3' }">
                        <UFormField label="Search text">
                            <UFieldGroup class="w-full">
                                <UInput placeholder="one piece" class="grow" />
                                <UButton label="Test" icon="i-lucide-flask-conical" color="neutral" />
                            </UFieldGroup>
                        </UFormField>
                    </UCard>
                    <UCard variant="outline">
                        <template #header>
                            <h3 class="font-semibold">Test results</h3>
                            <p class="text-sm text-muted">What the selectors extract from the real website.</p>
                        </template>
                        <UEmpty
                            icon="i-lucide-flask-conical"
                            title="Nothing tested yet"
                            description="Use the Test button above to try this step on the website."
                            variant="naked"
                            size="sm" />
                    </UCard>
                </div>

                <!-- loading -->
                <div class="flex flex-col gap-4">
                    <p class="text-xs font-medium tracking-wide text-dimmed uppercase">Loading</p>
                    <UCard variant="outline" :ui="{ body: 'p-3 sm:p-3' }">
                        <UFormField label="Search text">
                            <UFieldGroup class="w-full">
                                <UInput model-value="family wedding" class="grow" />
                                <UButton label="Test" icon="i-lucide-flask-conical" color="neutral" loading />
                            </UFieldGroup>
                        </UFormField>
                    </UCard>
                    <UCard variant="outline">
                        <template #header>
                            <h3 class="font-semibold">Test results</h3>
                            <p class="text-sm text-muted">Search test for “family wedding”</p>
                        </template>
                        <div class="flex flex-col gap-3">
                            <USkeleton class="h-16 w-full" />
                            <UCard v-for="n in 2" :key="n" variant="subtle" :ui="{ body: 'p-3 sm:p-3' }">
                                <div class="flex flex-col gap-2">
                                    <USkeleton class="h-3 w-12" />
                                    <USkeleton class="h-4 w-40" />
                                    <USkeleton class="h-3 w-12" />
                                    <USkeleton class="h-4 w-full" />
                                    <USkeleton class="h-7 w-36" />
                                </div>
                            </UCard>
                        </div>
                    </UCard>
                </div>

                <!-- error -->
                <div class="flex flex-col gap-4">
                    <p class="text-xs font-medium tracking-wide text-dimmed uppercase">Error</p>
                    <UCard variant="outline" :ui="{ body: 'p-3 sm:p-3' }">
                        <UFormField label="Search text">
                            <UFieldGroup class="w-full">
                                <UInput model-value="one piece" class="grow" />
                                <UButton label="Test" icon="i-lucide-flask-conical" color="neutral" />
                            </UFieldGroup>
                        </UFormField>
                    </UCard>
                    <UCard variant="outline">
                        <template #header>
                            <h3 class="font-semibold">Test results</h3>
                            <p class="text-sm text-muted">Search test for “one piece”</p>
                        </template>
                        <div class="flex flex-col gap-3">
                            <UAlert
                                color="error"
                                variant="subtle"
                                icon="i-lucide-circle-alert"
                                title="This step cannot be tested yet"
                                description="Search URL is required. Search result item selector is required." />
                            <UAlert
                                color="warning"
                                variant="subtle"
                                icon="i-lucide-triangle-alert"
                                title="1 of 5 matched elements were skipped because they have no title, URL or cover." />
                        </div>
                    </UCard>
                </div>

                <!-- success -->
                <div class="flex flex-col gap-4">
                    <p class="text-xs font-medium tracking-wide text-dimmed uppercase">Success</p>
                    <UCard variant="outline" :ui="{ body: 'p-3 sm:p-3' }">
                        <UFormField label="Search text">
                            <UFieldGroup class="w-full">
                                <UInput model-value="family wedding" class="grow" />
                                <UButton label="Test" icon="i-lucide-flask-conical" color="neutral" />
                            </UFieldGroup>
                        </UFormField>
                    </UCard>
                    <UCard variant="outline">
                        <template #header>
                            <div class="flex items-center justify-between gap-2">
                                <h3 class="font-semibold">Test results</h3>
                                <UBadge color="success" variant="subtle" label="HTTP 200" />
                            </div>
                            <p class="text-sm text-muted">Search test for “family wedding”</p>
                        </template>
                        <div class="flex flex-col gap-3">
                            <UAlert color="success" variant="subtle" icon="i-lucide-check" title="2 item(s) extracted" />
                            <UCard v-for="item in results" :key="item.title" variant="subtle" :ui="{ body: 'p-3 sm:p-3' }">
                                <dl class="flex flex-col gap-1.5 text-sm">
                                    <div>
                                        <dt class="text-xs font-medium text-muted">Title</dt>
                                        <dd>{{ item.title }}</dd>
                                    </div>
                                    <div>
                                        <dt class="text-xs font-medium text-muted">Url</dt>
                                        <dd class="break-all text-primary">{{ item.url }}</dd>
                                    </div>
                                </dl>
                                <template #footer>
                                    <UButton
                                        label="Use for chapters test"
                                        icon="i-lucide-arrow-down-to-line"
                                        color="neutral"
                                        variant="subtle"
                                        size="xs" />
                                </template>
                            </UCard>
                        </div>
                    </UCard>
                </div>
            </div>
        </section>

        <USeparator />

        <!-- --------------------------------------------------------------------------------------------- splitter -->
        <section id="splitter" class="flex flex-col gap-4">
            <div>
                <h2 class="text-lg font-semibold">2. A resizable split for the form</h2>
                <p class="text-sm text-muted">
                    Replaces: <code>grid grid-cols-1 lg:grid-cols-3</code> around the stepper and the sidebar. The divider is draggable (and
                    keyboard operable); <code>auto-save-id</code> remembers the width. The handle sits between the two panels.
                </p>
            </div>
            <div class="h-[470px] overflow-hidden rounded-lg border border-default">
                <USplitter id="palette-split" :items="splitItems">
                    <template #main>
                        <div class="flex h-full w-full min-w-0 flex-col gap-4 overflow-hidden p-5">
                            <UStepper v-model="step" :items="steps" :linear="false" class="w-full" />
                            <UFormField label="Search URL" required description="{query} is replaced with the URL-encoded search text.">
                                <UInput model-value="/search?name={query}" class="w-full" :ui="{ base: 'font-mono' }" />
                            </UFormField>
                            <UFormField label="Result item selector" required>
                                <UInput model-value="li:has(a.manga_cover)" class="w-full" :ui="{ base: 'font-mono' }" />
                            </UFormField>
                            <div class="flex justify-between gap-2">
                                <UButton label="Back" icon="i-lucide-arrow-left" color="neutral" variant="outline" />
                                <UButton label="Next" trailing-icon="i-lucide-arrow-right" color="neutral" variant="outline" />
                            </div>
                        </div>
                    </template>
                    <template #side>
                        <div class="flex h-full w-full min-w-0 flex-col gap-4 overflow-hidden bg-elevated/40 p-5">
                            <UCard variant="outline" :ui="{ body: 'p-3 sm:p-3' }">
                                <UFormField label="Search text">
                                    <UFieldGroup class="w-full">
                                        <UInput model-value="family wedding" class="grow" />
                                        <UButton label="Test" icon="i-lucide-flask-conical" color="neutral" />
                                    </UFieldGroup>
                                </UFormField>
                            </UCard>
                            <UCard variant="outline">
                                <template #header><h3 class="font-semibold">Test results</h3></template>
                                <UAlert color="success" variant="subtle" icon="i-lucide-check" title="1 item(s) extracted" />
                            </UCard>
                        </div>
                    </template>
                </USplitter>
            </div>
        </section>

        <USeparator />

        <!-- ---------------------------------------------------------------------------------------------- loading -->
        <section id="loading" class="flex flex-col gap-4">
            <div>
                <h2 class="text-lg font-semibold">3. Skeletons while loading</h2>
                <p class="text-sm text-muted">
                    Replaces: blank space or a spinner while a list loads. The same shape as the finished row, so the page does not jump.
                </p>
            </div>
            <div class="grid grid-cols-1 gap-6 lg:grid-cols-3">
                <div class="flex flex-col gap-3">
                    <p class="text-xs font-medium tracking-wide text-dimmed uppercase">Sources list</p>
                    <UCard v-for="n in 3" :key="n" variant="outline" :ui="{ body: 'p-4 sm:p-4' }">
                        <div class="flex items-center gap-4">
                            <USkeleton class="size-8 rounded-full" />
                            <div class="flex grow flex-col gap-2">
                                <USkeleton class="h-4 w-40" />
                                <USkeleton class="h-3 w-24" />
                            </div>
                            <USkeleton class="h-8 w-16" />
                        </div>
                    </UCard>
                </div>
                <div class="flex flex-col gap-3">
                    <p class="text-xs font-medium tracking-wide text-dimmed uppercase">Manga cards (start page)</p>
                    <div class="grid grid-cols-3 gap-3">
                        <div v-for="n in 3" :key="n" class="flex flex-col gap-2">
                            <USkeleton class="aspect-[2/3] w-full" />
                            <USkeleton class="h-4 w-full" />
                            <USkeleton class="h-3 w-2/3" />
                        </div>
                    </div>
                </div>
                <div class="flex flex-col gap-3">
                    <p class="text-xs font-medium tracking-wide text-dimmed uppercase">Table rows (tasks, workers)</p>
                    <UCard variant="outline" :ui="{ body: 'p-0 sm:p-0' }">
                        <div v-for="n in 5" :key="n" class="flex items-center gap-3 border-b border-default px-4 py-3 last:border-b-0">
                            <USkeleton class="h-4 w-4" />
                            <USkeleton class="h-4 grow" />
                            <USkeleton class="h-5 w-16 rounded-full" />
                        </div>
                    </UCard>
                </div>
            </div>
        </section>

        <USeparator />

        <!-- ------------------------------------------------------------------------------------------ empty states -->
        <section id="empty" class="flex flex-col gap-4">
            <div>
                <h2 class="text-lg font-semibold">4. Empty states and notices</h2>
                <p class="text-sm text-muted">Replaces: one-off paragraphs (“No manga found…”) that look different on each page.</p>
            </div>
            <div class="grid grid-cols-1 gap-6 lg:grid-cols-3">
                <UCard variant="outline">
                    <UEmpty
                        icon="i-lucide-globe"
                        title="No custom sources yet"
                        description="Add a website with CSS or XPath selectors, or start from an example."
                        variant="naked"
                        :actions="[
                            { label: 'Add custom source', icon: 'i-lucide-plus' },
                            { label: 'Examples', color: 'neutral', variant: 'subtle' },
                        ]" />
                </UCard>
                <UCard variant="outline">
                    <UEmpty
                        icon="i-lucide-library-big"
                        title="No manga found"
                        description="Search for a title to add your first manga."
                        variant="naked"
                        :actions="[{ label: 'Search', icon: 'i-lucide-search' }]" />
                </UCard>
                <UCard variant="outline">
                    <UEmpty
                        icon="i-lucide-search-x"
                        title="No matches"
                        description="The selector matched no elements. The site may render this part with JavaScript."
                        variant="naked" />
                </UCard>
            </div>
        </section>

        <USeparator />

        <!-- ---------------------------------------------------------------------------------------- small structure -->
        <section id="structure" class="flex flex-col gap-4">
            <div>
                <h2 class="text-lg font-semibold">5. Smaller pieces</h2>
                <p class="text-sm text-muted">Replaces: hand-made flex rows, bordered boxes and icon-only toggles.</p>
            </div>
            <div class="grid grid-cols-1 gap-6 lg:grid-cols-3">
                <UCard variant="outline">
                    <template #header><h3 class="font-semibold">Source rows as page cards</h3></template>
                    <div class="flex flex-col gap-3">
                        <UPageCard
                            v-for="source in sources"
                            :key="source.name"
                            variant="outline"
                            :title="source.name"
                            :description="source.url"
                            :ui="{ container: 'p-4 sm:p-4' }">
                            <div class="flex items-center gap-2">
                                <UBadge
                                    :color="source.custom ? 'primary' : 'neutral'"
                                    variant="subtle"
                                    :label="source.custom ? 'Custom' : 'Built-in'" />
                                <template v-if="source.custom">
                                    <UButton icon="i-lucide-pencil" label="Edit" size="xs" color="neutral" variant="outline" />
                                    <UButton icon="i-lucide-trash-2" label="Delete" size="xs" color="error" variant="subtle" />
                                </template>
                            </div>
                        </UPageCard>
                    </div>
                </UCard>

                <UCard variant="outline">
                    <template #header><h3 class="font-semibold">Field with advanced options</h3></template>
                    <div class="flex flex-col gap-4">
                        <UFormField label="Chapter number" description="Leave empty to search the whole item.">
                            <UFieldGroup class="w-full">
                                <UInput model-value="a" placeholder="CSS selector or XPath" class="grow" :ui="{ base: 'font-mono' }" />
                                <UInput model-value="href" placeholder="Attribute" class="w-32" :ui="{ base: 'font-mono' }" />
                            </UFieldGroup>
                        </UFormField>
                        <UCollapsible default-open>
                            <UButton
                                label="Regex and replacement"
                                color="neutral"
                                variant="ghost"
                                size="xs"
                                trailing-icon="i-lucide-chevron-down" />
                            <template #content>
                                <div class="flex flex-col gap-2 pt-2">
                                    <UInput
                                        model-value="/c(\d+(?:\.\d+)?)/?$"
                                        placeholder="Regex"
                                        :ui="{ base: 'font-mono' }"
                                        class="w-full" />
                                    <UInput placeholder="Replace with" :ui="{ base: 'font-mono' }" class="w-full" />
                                </div>
                            </template>
                        </UCollapsible>
                        <USeparator label="Request headers" />
                        <div class="flex gap-2">
                            <UInput placeholder="Header name" class="w-40" />
                            <UInput placeholder="Value" class="grow" />
                            <UButton icon="i-lucide-trash-2" color="neutral" variant="ghost" aria-label="Remove header" />
                        </div>
                    </div>
                </UCard>

                <UCard variant="outline">
                    <template #header><h3 class="font-semibold">Pattern test as a small card</h3></template>
                    <div class="flex flex-col gap-3">
                        <UFormField label="Series URL pattern">
                            <UInput
                                model-value="^https?://(?:www\.)?mangatown\.com/manga/(?&lt;id&gt;[^/?#]+)/?$"
                                class="w-full"
                                :ui="{ base: 'font-mono text-xs' }" />
                        </UFormField>
                        <UInput model-value="https://www.mangatown.com/manga/vagabond/" class="w-full" />
                        <UAlert color="success" variant="subtle" icon="i-lucide-check" title="Matches.">
                            <template #description>
                                <UBadge color="neutral" variant="subtle" class="font-mono" label="{id} = vagabond" />
                            </template>
                        </UAlert>
                        <UButton
                            label="Use for the chapters test"
                            icon="i-lucide-arrow-down-to-line"
                            color="neutral"
                            variant="subtle"
                            size="xs"
                            class="self-start" />
                    </div>
                </UCard>
            </div>
        </section>
    </div>
</template>

<script setup lang="ts">
import type { SplitterItem, StepperItem } from '@nuxt/ui';

definePageMeta({ layout: false });

const results = [
    { title: 'Naruto dj - Family Wedding', url: 'https://www.mangatown.com/manga/naruto_dj_family_wedding/' },
    { title: 'Naruto dj - Kakashi x Iruka', url: 'https://www.mangatown.com/manga/naruto_dj_kakashi_x_iruka/' },
];

const sources = [
    { name: 'MangaDex', url: 'https://mangadex.org', custom: false },
    { name: 'MANGA Plus', url: 'https://mangaplus.shueisha.co.jp', custom: false },
    { name: 'MangaTown', url: 'https://www.mangatown.com', custom: true },
];

const splitItems: SplitterItem[] = [
    { id: 'main', slot: 'main', defaultSize: 66, minSize: 40 },
    { id: 'side', slot: 'side', defaultSize: 34, minSize: 22 },
];

const steps: StepperItem[] = [
    { value: 'general', title: 'General', description: 'The website', icon: 'i-lucide-globe' },
    { value: 'search', title: 'Search', description: 'Find series', icon: 'i-lucide-search' },
    { value: 'chapters', title: 'Chapters', description: 'List chapters', icon: 'i-lucide-list-ordered' },
    { value: 'pages', title: 'Pages', description: 'Find images', icon: 'i-lucide-images' },
];
const step = ref('search');
</script>
