export default defineNuxtPlugin({
    name: 'apiBaseUrl',
    async setup(nuxtApp) {
        const configuredBaseUrl = nuxtApp.$config.public.api.baseUrl;
        const apiBaseUrl = configuredBaseUrl ? `http://${configuredBaseUrl}/api` : '/api';
        return { provide: { apiBaseUrl } };
    },
});
