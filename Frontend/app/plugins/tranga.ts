import { clearAuthToken, getAuthToken } from '~/composables/authToken';

export default defineNuxtPlugin({
    name: 'tranga-api',
    dependsOn: ['apiBaseUrl'],
    async setup(nuxtApp) {
        const tranga = $fetch.create({
            baseURL: nuxtApp.$apiBaseUrl as string,
            onRequest({ options }) {
                const token = getAuthToken();
                if (token) {
                    options.headers.set('Authorization', `Bearer ${token}`);
                }
            },
            onResponseError({ response }) {
                if (response.status === 401) {
                    clearAuthToken();
                    if (import.meta.client) void navigateTo('/login');
                }
            },
        });

        return { provide: { tranga } };
    },
});
