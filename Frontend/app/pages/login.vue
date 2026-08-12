<template>
    <div class="flex min-h-screen items-center justify-center p-4">
        <UCard class="w-full max-w-sm">
            <template #header>
                <div class="flex items-center gap-2">
                    <img src="/blahaj.png" class="h-8" alt="Blahaj" />
                    <p class="text-xl font-bold">Log in</p>
                </div>
            </template>

            <UForm :state="state" class="flex flex-col gap-4" @submit="submit">
                <UFormField label="Password" name="password">
                    <UInput v-model="state.password" type="password" class="w-full" autofocus />
                </UFormField>
                <UButton label="Log in" type="submit" loading-auto block />
            </UForm>
        </UCard>
    </div>
</template>

<script setup lang="ts">
import type { ServicesAuthAuthTokenResponse } from '~/api/tranga';
import { setAuthToken } from '~/composables/authToken';
import { FetchError } from 'ofetch';

const toast = useToast();
const state = ref<{ password?: string }>({});

const submit = async () => {
    if (!state.value.password) return;

    try {
        const response = await useNuxtApp().$tranga<ServicesAuthAuthTokenResponse>('/auth/login', {
            method: 'post',
            body: { password: state.value.password },
        });
        setAuthToken(response.token);
        await navigateTo('/');
    } catch (error: unknown) {
        const description = error instanceof FetchError ? (error.data ?? error.message) : 'Incorrect password.';
        toast.add({ title: 'Login failed.', description, color: 'error' });
    }
};
</script>
