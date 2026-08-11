<template>
    <TrangaPage>
        <UPageList class="gap-2">
            <UCollapsible :open="open">
                <UButton
                    label="Add Library"
                    color="neutral"
                    variant="subtle"
                    trailing-icon="i-lucide-chevron-down"
                    block
                    @click="open = !open" />

                <template #content>
                    <UCard class="m-0.5 mt-2">
                        <div class="flex flex-col gap-4">
                            <UForm :state="state" class="flex flex-col gap-4">
                                <UFormField label="Name" name="name">
                                    <UInput v-model="state.name" class="w-full" />
                                </UFormField>
                                <UFormField label="Base URL" name="baseUrl">
                                    <UInput v-model="state.baseUrl" class="w-full" />
                                </UFormField>
                                <UFormField
                                    label="Library Root Path"
                                    name="libraryRootPath"
                                    description="Path inside the Komga container where Tranga's manga volume is mounted. Defaults to /tranga.">
                                    <UInput v-model="state.libraryRootPath" placeholder="/tranga" class="w-full" />
                                </UFormField>
                            </UForm>
                            <UTabs v-model="active" :items="items" orientation="vertical" value-key="slot" class="h-40">
                                <template #credentials>
                                    <UForm :state="state" class="flex flex-col gap-4">
                                        <UFormField label="Username" name="username">
                                            <UInput v-model="state.username" class="w-full" />
                                        </UFormField>
                                        <UFormField label="Password" name="password">
                                            <UInput v-model="state.password" class="w-full" type="password" />
                                        </UFormField>
                                    </UForm>
                                </template>

                                <template #apiKey>
                                    <UForm :state="state" class="flex flex-col gap-4">
                                        <UFormField label="API Key" name="apiKey">
                                            <UInput v-model="state.apiKey" class="w-full" type="password" />
                                        </UFormField>
                                    </UForm>
                                </template>
                            </UTabs>
                            <div class="flex flex-row items-center gap-4 justify-end">
                                <span v-if="connectionVerified" class="flex items-center gap-1 text-sm text-success">
                                    <UIcon name="i-lucide-check" />
                                    Connection verified
                                </span>
                                <UButton
                                    label="Test Connection"
                                    color="neutral"
                                    variant="soft"
                                    loading-auto
                                    icon="i-lucide-plug"
                                    @click="testConnection" />
                                <UButton
                                    label="Cancel"
                                    color="secondary"
                                    variant="soft"
                                    @click="
                                        () => {
                                            state = {};
                                            active = 'credentials';
                                            connectionVerified = false;
                                            open = false;
                                        }
                                    " />
                                <UButton label="Add" loading-auto icon="i-lucide-plus" @click="createLibrary" />
                            </div>
                        </div>
                    </UCard>
                </template>
            </UCollapsible>
            <LibraryExtensionCard v-for="library in libraries" :key="library.id" :library="library" />
        </UPageList>
    </TrangaPage>
</template>

<script setup lang="ts">
import type { GetLibrariesResponse, PutLibrariesKomgaResponses, PostLibrariesKomgaTestConnectionResponses } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';
import { FetchError } from 'ofetch';
import Libraries = ApiKeys.Libraries;

const toast = useToast();

const open = ref(false);
const active = ref<string>('credentials');
const items = [
    { label: 'Username & Password', slot: 'credentials' },
    { label: 'API Key', slot: 'apiKey' },
];

const { data: libraries } = useTranga<GetLibrariesResponse>('/libraries', { key: Libraries.Libraries });

const state = ref<{ name?: string; baseUrl?: string; libraryRootPath?: string; username?: string; password?: string; apiKey?: string }>({});

const connectionVerified = ref(false);

watch(
    state,
    () => {
        connectionVerified.value = false;
    },
    { deep: true },
);

const testConnection = async () => {
    try {
        await useNuxtApp().$tranga<PostLibrariesKomgaTestConnectionResponses>('/libraries/komga/test-connection', {
            method: 'post',
            body:
                active.value === 'apiKey'
                    ? { baseUrl: state.value.baseUrl, apiKey: state.value.apiKey }
                    : { baseUrl: state.value.baseUrl, username: state.value.username, password: state.value.password },
        });
        connectionVerified.value = true;
        toast.add({ title: 'Connection verified.', color: 'success' });
    } catch (error: unknown) {
        connectionVerified.value = false;
        const description = error instanceof FetchError ? (error.data ?? error.message) : 'Could not verify the connection.';
        toast.add({ title: 'Connection test failed.', description, color: 'error' });
    }
};

const createLibrary = async () => {
    try {
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
        const description = error instanceof FetchError ? (error.data ?? error.message) : 'Could not add the library.';
        toast.add({ title: 'Failed adding library.', description, color: 'error' });
        return;
    }

    await refreshNuxtData(ApiKeys.Libraries.Libraries);
    toast.add({ title: 'Added library.', color: 'success' });
    state.value = {};
    active.value = 'credentials';
    connectionVerified.value = false;
    open.value = false;
};
</script>
