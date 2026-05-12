export default defineNuxtPlugin({
    name: 'apiBaseUrl',
    async setup(nuxtApp) {
        const apiBaseUrl = `http://${nuxtApp.$config.public.api.baseUrl}/api`;
        return { provide: { apiBaseUrl } };
    },
});
