export default defineNuxtPlugin({
    name: 'tranga-api',
    dependsOn: ['apiBaseUrl'],
    async setup(nuxtApp) {
        const tranga = $fetch.create({ baseURL: nuxtApp.$apiBaseUrl });

        return { provide: { tranga } };
    },
});
