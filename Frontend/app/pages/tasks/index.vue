<template>
    <TrangaPage rimless>
        <UPageSection :ui="{ container: 'py-2 sm:py-2 lg:py-4 px-0 sm:px-0 lg:px-0\' ' }">
            <div class="flex flex-wrap gap-4 items-end">
                <UFormField label="Manga">
                    <UFieldGroup>
                        <USelectMenu
                            v-model="mangaFilter"
                            :items="mangaOptions"
                            :loading="mangaStatus === 'pending'"
                            value-key="value"
                            searchable
                            placeholder="All Mangas"
                            class="w-56" />
                        <UButton v-if="mangaFilter" icon="i-lucide-x" color="neutral" variant="outline" @click="mangaFilter = undefined" />
                    </UFieldGroup>
                </UFormField>

                <UFormField label="Type">
                    <USelectMenu
                        v-model="typeFilter"
                        :items="typeOptions"
                        value-key="value"
                        multiple
                        placeholder="All Types"
                        class="w-56" />
                </UFormField>

                <UFormField label="State">
                    <USelectMenu
                        v-model="stateFilter"
                        :items="stateOptions"
                        value-key="value"
                        multiple
                        placeholder="All States"
                        class="w-56" />
                </UFormField>

                <UButton
                    v-if="mangaFilter || typeFilter.length || stateFilter.length"
                    label="Clear filters"
                    icon="i-lucide-filter-x"
                    color="neutral"
                    variant="ghost"
                    @click="clearFilters" />
            </div>
        </UPageSection>
        <UPageSection :ui="{ container: 'py-0 sm:py-0 lg:py-0 px-0 sm:px-0 lg:px-0' }">
            <TasksList ref="listRef" :tasks="tasks" :loading="status !== 'success' && tasks.length === 0" />
        </UPageSection>
    </TrangaPage>
</template>

<script setup lang="ts">
import { useInfiniteScroll } from '@vueuse/core';
import type { GetTasksResponse, GetMangasResponse, ServicesTasksTask, ServicesTasksTaskState } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';

const route = useRoute();
const router = useRouter();

function setQuery(key: string, value: string | undefined) {
    setQueries({ [key]: value });
}

function setQueries(values: Record<string, string | undefined>) {
    const query = Object.fromEntries(Object.entries(route.query).filter(([k]) => !(k in values)));
    for (const [key, value] of Object.entries(values)) if (value) query[key] = value;
    router.replace({ query });
}

const mangaFilter = computed<string | undefined>({
    get: () => (route.query.manga as string) || undefined,
    set: (v) => setQuery('manga', v),
});

const typeFilter = computed<string[]>({
    get: () => (route.query.type ? String(route.query.type).split(',').filter(Boolean) : []),
    set: (v) => setQuery('type', v.length ? v.join(',') : undefined),
});

const stateFilter = computed<ServicesTasksTaskState[]>({
    get: () => (route.query.state ? (String(route.query.state).split(',').filter(Boolean) as ServicesTasksTaskState[]) : []),
    set: (v) => setQuery('state', v.length ? v.join(',') : undefined),
});

const clearFilters = () => setQueries({ manga: undefined, type: undefined, state: undefined });

// The full set of concrete Task subtypes (Services.Tasks/Tasks/*.cs) - static rather than derived from
// loaded data, since only a page of tasks is loaded at a time under infinite scroll.
const TASK_TYPE_NAMES = [
    'DownloadChapterTask',
    'GetMangaChaptersTask',
    'MissingChapterScanTask',
    'PeriodicMangaChapterFetcherTask',
    'DbFileCleanupTask',
];
const typeOptions = TASK_TYPE_NAMES.map((n) => ({ label: taskTypeLabel(n), value: n })).sort((a, b) => a.label.localeCompare(b.label));

const stateOptions: { label: string; description: string; value: ServicesTasksTaskState }[] = (
    ['Pending', 'Blocked', 'Queued', 'Running', 'Completed', 'Failed'] as ServicesTasksTaskState[]
).map((s) => ({ label: s, description: taskStateDescription(s), value: s }));

const LIMIT = 25;
const skip = ref(0);
const tasks = ref<ServicesTasksTask[]>([]);
const hasMore = ref(true);

function buildQuery(overrides: { skip?: number; limit?: number } = {}) {
    const params = new URLSearchParams({
        includeFinished: 'true',
        skip: String(overrides.skip ?? skip.value),
        limit: String(overrides.limit ?? LIMIT),
    });
    if (mangaFilter.value) params.set('mangaId', mangaFilter.value);
    for (const t of typeFilter.value) params.append('taskTypeName', t);
    for (const s of stateFilter.value) params.append('status', s);
    return params;
}

const { data, status } = await useTranga<GetTasksResponse>(() => `/tasks?${buildQuery()}`, { lazy: true });

watch(data, (batch) => {
    const items = batch ?? [];
    tasks.value = skip.value === 0 ? items : [...tasks.value, ...items];
    hasMore.value = items.length >= LIMIT;
});

// Reset back to the first batch whenever a filter changes.
watch([mangaFilter, typeFilter, stateFilter], () => {
    skip.value = 0;
    tasks.value = [];
    hasMore.value = true;
});

const { data: mangas, status: mangaStatus } = await useTranga<GetMangasResponse>('/mangas', { key: ApiKeys.Manga.List, lazy: true });

const mangaOptions = computed(() =>
    (mangas.value ?? [])
        .map((m) => ({ label: m.metadataEntry?.series ?? m.mangaId, value: m.mangaId }))
        .sort((a, b) => a.label.localeCompare(b.label)),
);

const listRef = useTemplateRef('listRef');
onMounted(() => {
    useInfiniteScroll(
        () => listRef.value?.tableRef?.$el,
        () => {
            skip.value += LIMIT;
        },
        { distance: 200, canLoadMore: () => status.value !== 'pending' && hasMore.value },
    );
});

// Refresh the already-loaded rows in place, without disturbing scroll position or the "load more" cursor.
async function refreshLoaded() {
    if (tasks.value.length === 0) return;
    const { $tranga } = useNuxtApp();
    const refreshed = await $tranga<GetTasksResponse>(`/tasks?${buildQuery({ skip: 0, limit: tasks.value.length })}`);
    tasks.value = refreshed ?? [];
}

defineShortcuts({ meta_r: () => refreshLoaded() });

let interval: number;
onMounted(() => {
    interval = setInterval(() => refreshLoaded(), 5000);
});
onUnmounted(() => clearInterval(interval));
</script>
