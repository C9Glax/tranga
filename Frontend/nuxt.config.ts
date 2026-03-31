// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
    modules: ['@nuxt/eslint', '@nuxt/ui'],

    devtools: { enabled: true },

    css: ['~/assets/css/main.css'],

    routeRules: { '/': { prerender: true } },

    compatibilityDate: '2025-01-15',

    eslint: { config: { stylistic: { commaDangle: 'never', braceStyle: '1tbs' } } },

    app: { head: { title: 'Tranga', htmlAttrs: { lang: 'en' }, link: [{ rel: 'icon', type: 'image/png', href: '/blahaj.png' }] } },
});
