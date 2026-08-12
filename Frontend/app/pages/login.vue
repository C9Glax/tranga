<template>
    <AuthPage>
        <UAuthForm
            :schema="schema"
            :fields="fields"
            title="Log in"
            :submit="{ label: 'Log in' }"
            class="w-full max-w-sm"
            @submit="onSubmit" />
    </AuthPage>
</template>

<script setup lang="ts">
import type { ServicesAuthAuthTokenResponse } from '~/api/tranga';
import { setAuthToken } from '~/composables/authToken';
import { FetchError } from 'ofetch';
import * as z from 'zod';
import type { FormSubmitEvent } from '@nuxt/ui';

const toast = useToast();

const schema = z.object({ password: z.string().min(1, 'Password is required.') });

const fields = [{ name: 'password', label: 'Password', type: 'password' as const, required: true }];

const onSubmit = async (payload: FormSubmitEvent<z.infer<typeof schema>>) => {
    try {
        const response = await useNuxtApp().$tranga<ServicesAuthAuthTokenResponse>('/auth/login', {
            method: 'post',
            body: { password: payload.data.password },
        });
        setAuthToken(response.token);
        await navigateTo('/');
    } catch (error: unknown) {
        const description = error instanceof FetchError ? (error.data ?? error.message) : 'Incorrect password.';
        toast.add({ title: 'Login failed.', description, color: 'error' });
    }
};
</script>
