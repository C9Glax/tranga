<template>
    <UPageList class="gap-2">
        <UCollapsible :open="open">
            <UButton
                :label="$t('settings.libraries.add')"
                color="neutral"
                variant="subtle"
                trailing-icon="i-lucide-chevron-down"
                block
                @click="open = !open" />

            <template #content>
                <UCard class="m-0.5 mt-2">
                    <div class="flex flex-col gap-4">
                        <UForm :state="state" class="flex flex-col gap-4">
                            <UFormField :label="$t('settings.libraries.type')" name="type">
                                <USelect v-model="type" :items="typeItems" class="w-full" />
                            </UFormField>
                            <UFormField :label="$t('common.name')" name="name">
                                <UInput v-model="state.name" class="w-full" />
                            </UFormField>
                            <UFormField :label="$t('settings.libraries.baseUrl')" name="baseUrl">
                                <UInput v-model="state.baseUrl" class="w-full" />
                            </UFormField>
                            <UFormField
                                v-if="type === 'komga'"
                                :label="$t('settings.libraries.rootPath')"
                                name="libraryRootPath"
                                :description="$t('settings.libraries.rootPathDescription')">
                                <UInput v-model="state.libraryRootPath" placeholder="/tranga" class="w-full" />
                            </UFormField>
                        </UForm>
                        <UForm v-if="type === 'kavita'" :state="state" class="flex flex-col gap-4">
                            <UFormField
                                :label="$t('settings.libraries.authKey')"
                                name="apiKey"
                                :description="$t('settings.libraries.authKeyDescription')">
                                <UInput v-model="state.apiKey" class="w-full" type="password" />
                            </UFormField>
                            <UFormField
                                :label="$t('settings.libraries.kavitaLibrary')"
                                name="kavitaLibraryId"
                                :description="$t('settings.libraries.kavitaLibraryDescription')">
                                <USelect
                                    v-model="state.kavitaLibraryId"
                                    :items="kavitaLibraryItems"
                                    :disabled="kavitaLibraryItems.length === 0"
                                    :placeholder="$t('settings.libraries.kavitaLibraryPlaceholder')"
                                    class="w-full" />
                            </UFormField>
                        </UForm>
                        <UTabs v-else v-model="active" :items="items" orientation="vertical" value-key="slot" class="h-40">
                            <template #credentials>
                                <UForm :state="state" class="flex flex-col gap-4">
                                    <UFormField :label="$t('common.username')" name="username">
                                        <UInput v-model="state.username" class="w-full" />
                                    </UFormField>
                                    <UFormField :label="$t('common.password')" name="password">
                                        <UInput v-model="state.password" class="w-full" type="password" />
                                    </UFormField>
                                </UForm>
                            </template>

                            <template #apiKey>
                                <UForm :state="state" class="flex flex-col gap-4">
                                    <UFormField :label="$t('common.apiKey')" name="apiKey">
                                        <UInput v-model="state.apiKey" class="w-full" type="password" />
                                    </UFormField>
                                </UForm>
                            </template>
                        </UTabs>
                        <div class="flex flex-row items-center gap-4 justify-end">
                            <span v-if="connectionVerified" class="flex items-center gap-1 text-sm text-success">
                                <UIcon name="i-lucide-check" />
                                {{ $t('settings.libraries.connectionVerified') }}
                            </span>
                            <UButton
                                :label="$t('settings.libraries.testConnection')"
                                color="neutral"
                                variant="soft"
                                loading-auto
                                icon="i-lucide-plug"
                                @click="testConnection" />
                            <UButton
                                :label="$t('common.cancel')"
                                color="secondary"
                                variant="soft"
                                @click="
                                    () => {
                                        resetForm();
                                        open = false;
                                    }
                                " />
                            <UButton :label="$t('common.add')" loading-auto icon="i-lucide-plus" @click="createLibrary" />
                        </div>
                    </div>
                </UCard>
            </template>
        </UCollapsible>
        <LibraryExtensionCard v-for="library in libraries" :key="library.id" :library="library" />
    </UPageList>
</template>

<script setup lang="ts">
import type {
    GetLibrariesResponse,
    PutLibrariesKomgaResponses,
    PostLibrariesKomgaTestConnectionResponses,
    PutLibrariesKavitaResponses,
    PostLibrariesKavitaTestConnectionResponse,
    ServicesLibrariesKavitaLibraryInfo,
} from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';
import { FetchError } from 'ofetch';
import Libraries = ApiKeys.Libraries;

const { t } = useI18n();
const toast = useToast();

const open = ref(false);
const type = ref<'komga' | 'kavita'>('komga');
const typeItems = [
    { label: 'Komga', value: 'komga' },
    { label: 'Kavita', value: 'kavita' },
];
const active = ref<string>('credentials');
const kavitaLibraries = ref<ServicesLibrariesKavitaLibraryInfo[]>([]);
const kavitaLibraryItems = computed(() => kavitaLibraries.value.map((l) => ({ label: l.name, value: l.id })));
const items = computed(() => [
    { label: t('settings.libraries.usernameAndPassword'), slot: 'credentials' },
    { label: t('common.apiKey'), slot: 'apiKey' },
]);

const { data: libraries } = useTranga<GetLibrariesResponse>('/libraries', { key: Libraries.Libraries });

const state = ref<{
    name?: string;
    baseUrl?: string;
    libraryRootPath?: string;
    username?: string;
    password?: string;
    apiKey?: string;
    kavitaLibraryId?: string;
}>({});

const connectionVerified = ref(false);

const resetForm = () => {
    state.value = {};
    type.value = 'komga';
    active.value = 'credentials';
    kavitaLibraries.value = [];
    connectionVerified.value = false;
};

watch(
    state,
    () => {
        connectionVerified.value = false;
    },
    { deep: true },
);

const testConnection = async () => {
    try {
        if (type.value === 'kavita') {
            kavitaLibraries.value = await useNuxtApp().$tranga<PostLibrariesKavitaTestConnectionResponse>(
                '/libraries/kavita/test-connection',
                { method: 'post', body: { baseUrl: state.value.baseUrl, apiKey: state.value.apiKey } },
            );
            connectionVerified.value = true;
            toast.add({ title: t('settings.libraries.toast.connectionVerified'), color: 'success' });
            return;
        }

        await useNuxtApp().$tranga<PostLibrariesKomgaTestConnectionResponses>('/libraries/komga/test-connection', {
            method: 'post',
            body:
                active.value === 'apiKey'
                    ? { baseUrl: state.value.baseUrl, apiKey: state.value.apiKey }
                    : { baseUrl: state.value.baseUrl, username: state.value.username, password: state.value.password },
        });
        connectionVerified.value = true;
        toast.add({ title: t('settings.libraries.toast.connectionVerified'), color: 'success' });
    } catch (error: unknown) {
        connectionVerified.value = false;
        const description =
            error instanceof FetchError ? (error.data ?? error.message) : t('settings.libraries.toast.connectionFailedFallback');
        toast.add({ title: t('settings.libraries.toast.connectionFailed'), description, color: 'error' });
    }
};

const createLibrary = async () => {
    try {
        if (type.value === 'kavita') {
            await useNuxtApp().$tranga<PutLibrariesKavitaResponses>('/libraries/kavita', {
                method: 'put',
                body: {
                    name: state.value.name,
                    baseUrl: state.value.baseUrl,
                    apiKey: state.value.apiKey,
                    kavitaLibraryId: state.value.kavitaLibraryId,
                },
            });
        } else
            await useNuxtApp().$tranga<PutLibrariesKomgaResponses>('/libraries/komga', {
                method: 'put',
                body:
                    active.value === 'apiKey'
                        ? {
                              name: state.value.name,
                              baseUrl: state.value.baseUrl,
                              libraryRootPath: state.value.libraryRootPath,
                              apiKey: state.value.apiKey,
                          }
                        : {
                              name: state.value.name,
                              baseUrl: state.value.baseUrl,
                              libraryRootPath: state.value.libraryRootPath,
                              username: state.value.username,
                              password: state.value.password,
                          },
            });
    } catch (error: unknown) {
        const description = error instanceof FetchError ? (error.data ?? error.message) : t('settings.libraries.toast.addFailedFallback');
        toast.add({ title: t('settings.libraries.toast.addFailed'), description, color: 'error' });
        return;
    }

    await refreshNuxtData(ApiKeys.Libraries.Libraries);
    toast.add({ title: t('settings.libraries.toast.added'), color: 'success' });
    resetForm();
    open.value = false;
};
</script>
