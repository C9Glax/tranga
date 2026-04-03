export default defineNuxtPlugin((nuxtApp) => {
    const tranga = $fetch.create({ baseURL: `http://${nuxtApp.$config.public.api.baseUrl}` });

    return { provide: { tranga } };
});
