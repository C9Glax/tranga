<template>
    <AuthPage>
        <UAuthForm
            :schema="schema"
            :fields="fields"
            :title="$t('auth.login.title')"
            :submit="{ label: $t('auth.login.submit') }"
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

const schema = computed(() => z.object({ password: z.string().min(1, t('auth.passwordRequired')) }));

const fields = computed(() => [{ name: 'password', label: t('common.password'), type: 'password' as const, required: true }]);

const onSubmit = async (payload: FormSubmitEvent<z.infer<typeof schema.value>>) => {
    try {
        const response = await useNuxtApp().$tranga<ServicesAuthAuthTokenResponse>('/auth/login', {
            method: 'post',
            body: { password: payload.data.password },
        });
        setAuthToken(response.token);
        await navigateTo('/');
    } catch (error: unknown) {
        const description = error instanceof FetchError ? (error.data ?? error.message) : t('auth.login.incorrectPassword');
        toast.add({ title: t('auth.login.failed'), description, color: 'error' });
    }
};
</script>
