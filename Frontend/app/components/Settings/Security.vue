<template>
    <UPageList class="gap-4">
        <UCard>
            <template #header>
                <p class="font-semibold">{{ $t('settings.security.apiKeys') }}</p>
            </template>

            <div class="flex flex-col gap-4">
                <UFormField
                    v-if="createdKey"
                    :label="$t('settings.security.newKey')"
                    :description="$t('settings.security.newKeyDescription')">
                    <UInput :model-value="createdKey" readonly class="w-full">
                        <template #trailing>
                            <UButton
                                :color="keyCopied ? 'success' : 'neutral'"
                                variant="link"
                                size="sm"
                                :icon="keyCopied ? 'i-lucide-check' : 'i-lucide-copy'"
                                :aria-label="$t('settings.security.copyKey')"
                                @click="copyCreatedKey" />
                        </template>
                    </UInput>
                </UFormField>

                <UForm :state="newKeyState" class="flex flex-row items-end gap-4" @submit="createApiKey">
                    <UFormField :label="$t('settings.security.name')" name="name" class="flex-1">
                        <UInput v-model="newKeyState.name" :placeholder="$t('settings.security.namePlaceholder')" class="w-full" />
                    </UFormField>
                    <UButton :label="$t('settings.security.createKey')" type="submit" loading-auto icon="i-lucide-plus" />
                </UForm>

                <div v-if="apiKeys && apiKeys.length" class="flex flex-col gap-2">
                    <div
                        v-for="key in apiKeys"
                        :key="key.id"
                        class="flex items-center justify-between rounded-md border border-default p-3">
                        <div>
                            <p class="font-medium">{{ key.name ?? $t('common.unnamed') }}</p>
                            <p class="text-sm text-muted">
                                {{ $t('settings.security.created', { date: new Date(key.createdAt).toLocaleString(locale) }) }}
                            </p>
                        </div>
                        <UButton
                            :label="$t('settings.security.revoke')"
                            color="error"
                            variant="soft"
                            size="xs"
                            @click="revokeApiKey(key.id)" />
                    </div>
                </div>
                <UEmpty
                    v-else
                    icon="i-lucide-key-round"
                    :title="$t('settings.security.noKeys')"
                    :description="$t('settings.security.noKeysDescription')"
                    variant="naked"
                    size="sm" />
            </div>
        </UCard>
    </UPageList>
</template>

<script setup lang="ts">
import type { ServicesAuthApiKeyResponse, ServicesAuthCreateApiKeyResponse } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';
import { FetchError } from 'ofetch';

const { t, locale } = useI18n();
const toast = useToast();

const { data: apiKeys, refresh } = useTranga<ServicesAuthApiKeyResponse[]>('/auth/apikeys', { key: ApiKeys.Auth.ApiKeys });

const newKeyState = ref<{ name?: string }>({});
const createdKey = ref<string | null>(null);
const keyCopied = ref(false);

const copyCreatedKey = async () => {
    if (!createdKey.value) return;
    await navigator.clipboard.writeText(createdKey.value);
    keyCopied.value = true;
    setTimeout(() => (keyCopied.value = false), 2000);
};

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
        const description = error instanceof FetchError ? (error.data ?? error.message) : t('settings.security.createFailedFallback');
        toast.add({ title: t('settings.security.createFailed'), description, color: 'error' });
    }
};

const revokeApiKey = async (id: string) => {
    try {
        await useNuxtApp().$tranga(`/auth/apikeys/${id}`, { method: 'delete' });
        await refresh();
        toast.add({ title: t('settings.security.revoked'), color: 'success' });
    } catch (error: unknown) {
        const description = error instanceof FetchError ? (error.data ?? error.message) : t('settings.security.revokeFailedFallback');
        toast.add({ title: t('settings.security.revokeFailed'), description, color: 'error' });
    }
};
</script>
