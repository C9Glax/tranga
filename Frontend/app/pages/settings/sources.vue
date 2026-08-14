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
                <UCard>
                    <div class="flex flex-col gap-3">
                        <div class="flex flex-row flex-wrap items-center gap-2">
                            <UInput
                                v-model="query"
                                placeholder="Search extensions"
                                icon="i-lucide-search"
                                class="grow"
                                @update:model-value="page = 1" />
                            <USelect
                                v-model="language"
                                :items="languageItems"
                                class="w-40"
                                placeholder="Language"
                                @update:model-value="page = 1" />
                            <UButton
                                label="Refresh catalogue"
                                icon="i-lucide-refresh-cw"
                                color="neutral"
                                variant="soft"
                                loading-auto
                                @click="refreshCatalogue" />
                        </div>
                        <div class="flex flex-row flex-wrap items-center gap-4">
                            <USwitch v-model="installedOnly" label="Installed only" @update:model-value="page = 1" />
                            <USwitch v-model="showNsfw" label="Show NSFW" @update:model-value="page = 1" />
                            <span class="text-sm text-muted">{{ filtered.length }} of {{ extensions?.length ?? 0 }} extensions</span>
                        </div>
                    </div>
                </UCard>

                <UCard v-for="extension in paged" :key="extension.pkgName">
                    <div class="flex flex-row items-center gap-3">
                        <NuxtImg :src="extension.iconUrl" :alt="extension.name" class="size-10 rounded" />
                        <div class="flex flex-col grow min-w-0">
                            <div class="flex flex-row items-center gap-2 flex-wrap">
                                <span class="font-medium truncate">{{ extension.name }}</span>
                                <UBadge :label="extension.lang" variant="outline" color="neutral" />
                                <UBadge v-if="extension.isNsfw" label="NSFW" color="error" variant="solid" />
                                <UBadge v-if="extension.isObsolete" label="Obsolete" color="warning" variant="subtle" />
                                <UBadge v-if="extension.hasUpdate" label="Update available" color="info" variant="subtle" />
                            </div>
                            <span class="text-sm text-muted truncate">v{{ extension.versionName }} &middot; {{ extension.pkgName }}</span>
                        </div>
                        <div class="flex flex-row items-center gap-2">
                            <UButton
                                v-if="!extension.isInstalled"
                                label="Install"
                                icon="i-lucide-download"
                                loading-auto
                                @click="install(extension)" />
                            <UButton
                                v-if="extension.isInstalled && extension.hasUpdate"
                                label="Update"
                                icon="i-lucide-arrow-up"
                                color="info"
                                loading-auto
                                @click="update(extension)" />
                            <UButton
                                v-if="extension.isInstalled"
                                label="Uninstall"
                                icon="i-lucide-trash-2"
                                color="error"
                                variant="soft"
                                loading-auto
                                @click="uninstall(extension)" />
                        </div>
                    </div>
                </UCard>

                <UPagination v-if="filtered.length > pageSize" v-model:page="page" :total="filtered.length" :items-per-page="pageSize" />
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
import { ApiKeys } from '~/composables/ApiKeys';
import { FetchError } from 'ofetch';

const toast = useToast();

const { data: status } = await useTranga<GetMangasSuwayomiStatusResponse>('/mangas/suwayomi/status', { key: ApiKeys.SuwayomiStatus });

// The catalogue is ~1400 rows, but it is a flat list of small objects: one fetch and client-side filtering keeps the
// page responsive without paging the API.
const { data: extensions } = await useTranga<GetMangasSuwayomiExtensionsResponse>('/mangas/suwayomi/extensions', {
    key: ApiKeys.SuwayomiExtensions,
    immediate: false,
});

const query = ref('');
const language = ref<string>('all languages');
const installedOnly = ref(false);
const showNsfw = ref(false);
const page = ref(1);
const pageSize = 25;

watch(
    () => status.value?.reachable,
    (reachable) => {
        if (reachable) refreshNuxtData(ApiKeys.SuwayomiExtensions);
    },
    { immediate: true },
);

const languageItems = computed(() => ['all languages', ...new Set((extensions.value ?? []).map((e) => e.lang))].sort());

const filtered = computed(() => {
    const term = query.value.trim().toLowerCase();
    return (extensions.value ?? []).filter((extension) => {
        if (!showNsfw.value && extension.isNsfw) return false;
        if (installedOnly.value && !extension.isInstalled) return false;
        if (language.value !== 'all languages' && extension.lang !== language.value) return false;
        if (term && !extension.name.toLowerCase().includes(term) && !extension.pkgName.toLowerCase().includes(term)) return false;
        return true;
    });
});

const paged = computed(() => filtered.value.slice((page.value - 1) * pageSize, page.value * pageSize));

watch(filtered, () => {
    const lastPage = Math.max(1, Math.ceil(filtered.value.length / pageSize));
    if (page.value > lastPage) page.value = lastPage;
});

const refreshCatalogue = async () => {
    try {
        // refresh=true makes the sidecar re-read the configured extension stores, which hits the network.
        const fresh = await useNuxtApp().$tranga<GetMangasSuwayomiExtensionsResponse>('/mangas/suwayomi/extensions', {
            query: { refresh: true },
        });
        extensions.value = fresh;
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
