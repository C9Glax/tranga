export default defineNuxtPlugin({
    name: 'tranga-api',
    dependsOn: ['apiBaseUrl'],
    async setup(nuxtApp) {
        const tranga = $fetch.create({ baseURL: nuxtApp.$apiBaseUrl as string });

        return { provide: { tranga } };
    },
});
