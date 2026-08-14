<template>
    <TrangaPage>
        <UPageList class="gap-2">
            <UAlert
                v-if="status && !status.enabled"
                title="Suwayomi sidecar is disabled"
                description="Set ENABLE_SUWAYOMI=true to run the sidecar and get access to the keiyoushi extensions. On docker-compose also set COMPOSE_PROFILES=suwayomi."
                icon="i-lucide-plug-zap"
                color="neutral"
                variant="subtle" />

            <UAlert
                v-else-if="status && !status.reachable"
                title="Suwayomi sidecar is not responding"
                description="The sidecar is enabled but could not be reached. It may still be starting up."
                icon="i-lucide-triangle-alert"
                color="warning"
                variant="subtle" />

            <UAlert
                v-else-if="status"
                :title="`${status.serverName ?? 'Suwayomi'} ${status.serverVersion ?? ''}`.trim()"
                :description="`${status.installedSourceCount} source(s) available as download extensions.`"
                icon="i-lucide-check"
                color="success"
                variant="subtle">
                <template #actions>
                    <UButton label="Open Suwayomi" icon="i-lucide-external-link" variant="soft" to="/suwayomi/" target="_blank" />
                </template>
            </UAlert>

            <template v-if="status?.reachable">
                <div class="flex flex-row flex-wrap items-center gap-2">
                    <UInput v-model="query" placeholder="Search extensions" icon="i-lucide-search" class="grow" />
                    <USelect v-model="language" :items="languageItems" class="w-40" placeholder="Language" />
                    <UButton
                        label="Refresh catalogue"
                        icon="i-lucide-refresh-cw"
                        color="neutral"
                        variant="soft"
                        loading-auto
                        @click="refreshCatalogue" />
                </div>
                <div class="flex flex-row flex-wrap items-center gap-4">
                    <USwitch v-model="installedOnly" label="Installed only" />
                    <USwitch v-model="showNsfw" label="Show NSFW" />
                    <span class="text-sm text-muted">{{ filtered.length }} of {{ extensions?.length ?? 0 }} extensions</span>
                </div>

                <!-- The catalogue is ~1400 rows. Virtualizing keeps the DOM to the visible window, so the whole list
                     stays scrollable without paging it. `sticky` is deliberately absent: UTable does not support it
                     together with `virtualize`. -->
                <UTable
                    :data="filtered"
                    :columns="columns"
                    :loading="pending"
                    :virtualize="{ estimateSize: 64, overscan: 8 }"
                    class="w-full h-[70vh]">
                    <template #name-cell="{ row }">
                        <div class="flex flex-row items-center gap-3 min-w-0">
                            <img :src="row.original.iconUrl" :alt="row.original.name" class="size-8 shrink-0 rounded" />
                            <div class="flex flex-col min-w-0">
                                <span class="font-medium truncate">{{ row.original.name }}</span>
                                <span class="text-xs text-muted truncate">{{ row.original.pkgName }}</span>
                            </div>
                        </div>
                    </template>

                    <template #lang-cell="{ row }">
                        <UBadge :label="row.original.lang" variant="outline" color="neutral" />
                    </template>

                    <template #versionName-cell="{ row }"> v{{ row.original.versionName }} </template>

                    <template #state-cell="{ row }">
                        <div class="flex flex-row items-center gap-1">
                            <UBadge v-if="row.original.isInstalled" label="Installed" color="secondary" variant="subtle" />
                            <UBadge v-if="row.original.isNsfw" label="NSFW" color="error" variant="solid" />
                            <UBadge v-if="row.original.isObsolete" label="Obsolete" color="warning" variant="subtle" />
                            <UBadge v-if="row.original.hasUpdate" label="Update" color="info" variant="subtle" />
                        </div>
                    </template>

                    <template #actions-cell="{ row }">
                        <div class="flex flex-row items-center justify-end gap-2">
                            <UButton
                                v-if="!row.original.isInstalled"
                                label="Install"
                                icon="i-lucide-download"
                                size="sm"
                                loading-auto
                                @click="install(row.original)" />
                            <UButton
                                v-if="row.original.isInstalled && row.original.hasUpdate"
                                label="Update"
                                icon="i-lucide-arrow-up"
                                color="info"
                                size="sm"
                                loading-auto
                                @click="update(row.original)" />
                            <UButton
                                v-if="row.original.isInstalled"
                                label="Uninstall"
                                icon="i-lucide-trash-2"
                                color="error"
                                variant="soft"
                                size="sm"
                                loading-auto
                                @click="uninstall(row.original)" />
                        </div>
                    </template>
                </UTable>
            </template>
        </UPageList>
    </TrangaPage>
</template>

<script setup lang="ts">
import type {
    GetMangasSuwayomiStatusResponse,
    GetMangasSuwayomiExtensionsResponse,
    ServicesMangaSuwayomiExtensionInfo,
} from '~/api/tranga';
import type { TableColumn } from '@nuxt/ui/components/Table.vue';
import { ApiKeys } from '~/composables/ApiKeys';
import { FetchError } from 'ofetch';

const toast = useToast();

const { data: status } = await useTranga<GetMangasSuwayomiStatusResponse>('/mangas/suwayomi/status', { key: ApiKeys.SuwayomiStatus });

// Fetched unconditionally: the endpoint answers 503 when the sidecar is off, which useFetch surfaces as an error and
// the status alerts above already cover. Deferring this with `immediate: false` instead would hang the page, because
// awaiting a deferred useFetch never resolves.
const { data: extensions, pending } = await useTranga<GetMangasSuwayomiExtensionsResponse>('/mangas/suwayomi/extensions', {
    key: ApiKeys.SuwayomiExtensions,
});

const ALL_LANGUAGES = 'all languages';

const query = ref('');
const language = ref<string>(ALL_LANGUAGES);
const installedOnly = ref(false);
const showNsfw = ref(false);

const columns: TableColumn<ServicesMangaSuwayomiExtensionInfo>[] = [
    { accessorKey: 'name', header: 'Extension' },
    { accessorKey: 'lang', header: 'Language' },
    { accessorKey: 'versionName', header: 'Version' },
    { id: 'state', header: 'Status' },
    { id: 'actions', header: '' },
];

const languageItems = computed(() => [ALL_LANGUAGES, ...[...new Set((extensions.value ?? []).map((e) => e.lang))].sort()]);

const filtered = computed(() => {
    const term = query.value.trim().toLowerCase();
    return (extensions.value ?? []).filter((extension) => {
        if (!showNsfw.value && extension.isNsfw) return false;
        if (installedOnly.value && !extension.isInstalled) return false;
        if (language.value !== ALL_LANGUAGES && extension.lang !== language.value) return false;
        if (term && !extension.name.toLowerCase().includes(term) && !extension.pkgName.toLowerCase().includes(term)) return false;
        return true;
    });
});

const refreshCatalogue = async () => {
    try {
        // refresh=true makes the sidecar re-read the configured extension stores, which hits the network.
        extensions.value = await useNuxtApp().$tranga<GetMangasSuwayomiExtensionsResponse>('/mangas/suwayomi/extensions', {
            query: { refresh: true },
        });
        toast.add({ title: 'Extension catalogue refreshed.', color: 'success' });
    } catch (error: unknown) {
        toast.add({ title: 'Failed refreshing the catalogue.', description: describe(error), color: 'error' });
    }
};

const install = (extension: ServicesMangaSuwayomiExtensionInfo) =>
    act(`/mangas/suwayomi/extensions/${extension.pkgName}/install`, 'post', `Installed ${extension.name}.`, 'Failed installing extension.');

const update = (extension: ServicesMangaSuwayomiExtensionInfo) =>
    act(`/mangas/suwayomi/extensions/${extension.pkgName}/update`, 'post', `Updated ${extension.name}.`, 'Failed updating extension.');

const uninstall = (extension: ServicesMangaSuwayomiExtensionInfo) =>
    act(`/mangas/suwayomi/extensions/${extension.pkgName}`, 'delete', `Uninstalled ${extension.name}.`, 'Failed uninstalling extension.');

const act = async (url: string, method: 'post' | 'delete', success: string, failure: string) => {
    try {
        await useNuxtApp().$tranga(url, { method });
    } catch (error: unknown) {
        toast.add({ title: failure, description: describe(error), color: 'error' });
        return;
    }

    // The set of download extensions changed, so anything showing them has to be re-fetched too.
    await Promise.all([
        refreshNuxtData(ApiKeys.SuwayomiExtensions),
        refreshNuxtData(ApiKeys.SuwayomiStatus),
        refreshNuxtData(ApiKeys.DownloadExtensions),
    ]);
    toast.add({ title: success, color: 'success' });
};

const describe = (error: unknown) => (error instanceof FetchError ? (error.data ?? error.message) : undefined);
</script>
