<template>
    <TrangaPage>
        <UPageList class="gap-4">
            <UCard>
                <template #header>
                    <p class="font-semibold">API Keys</p>
                </template>

                <div class="flex flex-col gap-4">
                    <UAlert
                        v-if="createdKey"
                        color="success"
                        icon="i-lucide-key-round"
                        title="Copy this key now - it will not be shown again"
                        :description="createdKey" />

                    <UForm :state="newKeyState" class="flex flex-row items-end gap-4" @submit="createApiKey">
                        <UFormField label="Name" name="name" class="flex-1">
                            <UInput v-model="newKeyState.name" placeholder="e.g. backup script" class="w-full" />
                        </UFormField>
                        <UButton label="Create key" type="submit" loading-auto icon="i-lucide-plus" />
                    </UForm>

                    <div v-if="apiKeys && apiKeys.length" class="flex flex-col gap-2">
                        <div
                            v-for="key in apiKeys"
                            :key="key.id"
                            class="flex items-center justify-between rounded-md border border-default p-3">
                            <div>
                                <p class="font-medium">{{ key.name ?? '(unnamed)' }}</p>
                                <p class="text-sm text-muted">Created {{ new Date(key.createdAt).toLocaleString() }}</p>
                            </div>
                            <UButton label="Revoke" color="error" variant="soft" size="xs" @click="revokeApiKey(key.id)" />
                        </div>
                    </div>
                    <p v-else class="text-sm text-muted">No API keys yet.</p>
                </div>
            </UCard>
        </UPageList>
    </TrangaPage>
</template>

<script setup lang="ts">
import type { ServicesAuthApiKeyResponse, ServicesAuthCreateApiKeyResponse } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';
import { FetchError } from 'ofetch';

const toast = useToast();

const { data: apiKeys, refresh } = useTranga<ServicesAuthApiKeyResponse[]>('/auth/apikeys', { key: ApiKeys.Auth.ApiKeys });

const newKeyState = ref<{ name?: string }>({});
const createdKey = ref<string | null>(null);

const createApiKey = async () => {
    try {
        const response = await useNuxtApp().$tranga<ServicesAuthCreateApiKeyResponse>('/auth/apikeys', {
            method: 'post',
            body: { name: newKeyState.value.name ?? null, scope: 'All' },
        });
        createdKey.value = response.key;
        newKeyState.value = {};
        await refresh();
    } catch (error: unknown) {
        const description = error instanceof FetchError ? (error.data ?? error.message) : 'Could not create the API key.';
        toast.add({ title: 'Failed creating API key.', description, color: 'error' });
    }
};

const revokeApiKey = async (id: string) => {
    try {
        await useNuxtApp().$tranga(`/auth/apikeys/${id}`, { method: 'delete' });
        await refresh();
        toast.add({ title: 'API key revoked.', color: 'success' });
    } catch (error: unknown) {
        const description = error instanceof FetchError ? (error.data ?? error.message) : 'Could not revoke the API key.';
        toast.add({ title: 'Failed revoking API key.', description, color: 'error' });
    }
};
</script>
