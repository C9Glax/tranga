// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
    modules: ['@nuxt/eslint', '@nuxt/ui'],

    devtools: { enabled: true },

    app: { head: { title: 'Tranga', htmlAttrs: { lang: 'en' }, link: [{ rel: 'icon', type: 'image/png', href: '/blahaj.png' }] } },

    css: ['~/assets/css/main.css'],

    runtimeConfig: { public: { api: { baseUrl: 'localhost:5000' } } },

    compatibilityDate: '2025-01-15',

    vite: { server: { allowedHosts: ['host.docker.internal', 'aspire.dev.internal'] } },

    eslint: { config: { stylistic: { semi: true, arrowParens: true, braceStyle: '1tbs', indent: 4, commaDangle: 'always-multiline' } } },
});
