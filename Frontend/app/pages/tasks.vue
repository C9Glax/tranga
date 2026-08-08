<template>
    <TrangaPage :page-title="{ title: 'Tasks', icon: { name: 'i-lucide-biceps-flexed' } }" rimless>
        <UPageSection :ui="{ container: 'px-0 max-w-none sm:py-0 lg:py-0 gap-8 sm:gap-8 mb-8' }">
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
        <UPageSection :ui="{ container: 'px-0 max-w-none sm:py-0 lg:py-0 gap-8 sm:gap-8' }">
            <TasksList :tasks="filteredTasks" :loading="status !== 'success'" />
        </UPageSection>
    </TrangaPage>
</template>

<script setup lang="ts">
import type { GetTasksResponse, GetMangasResponse, ServicesTasksTaskState } from '~/api/tranga';
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

const { data, refresh, status } = await useTranga<GetTasksResponse>(() => `/tasks?includeFinished=true`, { lazy: true });

const { data: mangas, status: mangaStatus } = await useTranga<GetMangasResponse>('/mangas', { key: ApiKeys.Manga.List, lazy: true });

const mangaOptions = computed(() =>
    (mangas.value ?? [])
        .map((m) => ({ label: m.metadataEntry?.series ?? m.mangaId, value: m.mangaId }))
        .sort((a, b) => a.label.localeCompare(b.label))
);

const typeOptions = computed(() => {
    const names = new Set((data.value ?? []).map((t) => t.taskTypeName));
    return [...names].map((n) => ({ label: taskTypeLabel(n), value: n })).sort((a, b) => a.label.localeCompare(b.label));
});

const stateOptions: { label: string; description: string; value: ServicesTasksTaskState }[] = (
    ['Pending', 'Blocked', 'Queued', 'Running', 'Completed', 'Failed'] as ServicesTasksTaskState[]
).map((s) => ({ label: s, description: taskStateDescription(s), value: s }));

const filteredTasks = computed(() =>
    (data.value ?? []).filter((t) => {
        if (mangaFilter.value && t.mangaId !== mangaFilter.value) return false;
        if (typeFilter.value.length && !typeFilter.value.includes(t.taskTypeName)) return false;
        if (stateFilter.value.length && !stateFilter.value.includes(t.status)) return false;
        return true;
    })
);

defineShortcuts({ meta_r: () => refresh() });

let interval: number;
onMounted(() => {
    interval = setInterval(() => refresh(), 5000);
});
onUnmounted(() => clearInterval(interval));
</script>
