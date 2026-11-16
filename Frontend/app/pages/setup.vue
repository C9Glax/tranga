<template>
    <AuthPage>
        <UAuthForm
            :schema="schema"
            :fields="fields"
            :title="$t('auth.setup.title')"
            :description="$t('auth.setup.description')"
            :submit="{ label: $t('auth.setup.submit') }"
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

const { t } = useI18n();
const toast = useToast();

const schema = computed(() =>
    z
        .object({ password: z.string().min(8, t('auth.passwordTooShort')), confirmPassword: z.string() })
        .refine((data) => data.password === data.confirmPassword, { message: t('auth.passwordsDoNotMatch'), path: ['confirmPassword'] }),
);

const fields = computed(() => [
    { name: 'password', label: t('common.password'), type: 'password' as const, required: true },
    { name: 'confirmPassword', label: t('auth.setup.confirmPassword'), type: 'password' as const, required: true },
]);

const onSubmit = async (payload: FormSubmitEvent<z.infer<typeof schema.value>>) => {
    try {
        const response = await useNuxtApp().$tranga<ServicesAuthAuthTokenResponse>('/auth/setup', {
            method: 'post',
            body: { password: payload.data.password },
        });
        setAuthToken(response.token);
        await navigateTo('/');
    } catch (error: unknown) {
        const description = error instanceof FetchError ? (error.data ?? error.message) : t('auth.setup.couldNotCreate');
        toast.add({ title: t('auth.setup.failed'), description, color: 'error' });
    }
};
</script>
