<template>
    <div class="flex min-h-screen items-center justify-center p-4">
        <UCard class="w-full max-w-sm">
            <template #header>
                <div class="flex items-center gap-2">
                    <img src="/blahaj.png" class="h-8" alt="Blahaj" />
                    <p class="text-xl font-bold">Create admin password</p>
                </div>
            </template>

            <UForm :state="state" class="flex flex-col gap-4" @submit="submit">
                <UFormField label="Password" name="password">
                    <UInput v-model="state.password" type="password" class="w-full" autofocus />
                </UFormField>
                <UFormField label="Confirm password" name="confirmPassword">
                    <UInput v-model="state.confirmPassword" type="password" class="w-full" />
                </UFormField>
                <UButton label="Create password" type="submit" loading-auto block />
            </UForm>
        </UCard>
    </div>
</template>

<script setup lang="ts">
import type { ServicesAuthAuthTokenResponse } from '~/api/tranga';
import { setAuthToken } from '~/composables/authToken';
import { FetchError } from 'ofetch';

const toast = useToast();
const state = ref<{ password?: string; confirmPassword?: string }>({});

const submit = async () => {
    if (!state.value.password || state.value.password.length < 8) {
        toast.add({ title: 'Password must be at least 8 characters.', color: 'error' });
        return;
    }
    if (state.value.password !== state.value.confirmPassword) {
        toast.add({ title: 'Passwords do not match.', color: 'error' });
        return;
    }

    try {
        const response = await useNuxtApp().$tranga<ServicesAuthAuthTokenResponse>('/auth/setup', {
            method: 'post',
            body: { password: state.value.password },
        });
        setAuthToken(response.token);
        await navigateTo('/');
    } catch (error: unknown) {
        const description = error instanceof FetchError ? (error.data ?? error.message) : 'Could not create the password.';
        toast.add({ title: 'Setup failed.', description, color: 'error' });
    }
};
</script>
