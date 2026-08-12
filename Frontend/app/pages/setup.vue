<template>
    <AuthPage>
        <UAuthForm
            :schema="schema"
            :fields="fields"
            title="Create admin password"
            description="This is the only account for this instance - there are no user profiles."
            :submit="{ label: 'Create password' }"
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

const schema = z
    .object({ password: z.string().min(8, 'Password must be at least 8 characters.'), confirmPassword: z.string() })
    .refine((data) => data.password === data.confirmPassword, { message: 'Passwords do not match.', path: ['confirmPassword'] });

const fields = [
    { name: 'password', label: 'Password', type: 'password' as const, required: true },
    { name: 'confirmPassword', label: 'Confirm password', type: 'password' as const, required: true },
];

const onSubmit = async (payload: FormSubmitEvent<z.infer<typeof schema>>) => {
    try {
        const response = await useNuxtApp().$tranga<ServicesAuthAuthTokenResponse>('/auth/setup', {
            method: 'post',
            body: { password: payload.data.password },
        });
        setAuthToken(response.token);
        await navigateTo('/');
    } catch (error: unknown) {
        const description = error instanceof FetchError ? (error.data ?? error.message) : 'Could not create the password.';
        toast.add({ title: 'Setup failed.', description, color: 'error' });
    }
};
</script>
