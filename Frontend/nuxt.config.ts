import { gitDescribeSync } from 'git-describe';

// https://nuxt.com/docs/api/configuration/nuxt-config

const gitInfo = (() => {
    try {
        return gitDescribeSync({ dirtySemver: false });
    } catch {
        return null;
    }
})();

export default defineNuxtConfig({
    modules: ['@nuxt/eslint', '@nuxt/ui'],

    devtools: { enabled: true },

    app: { head: { title: 'Tranga', htmlAttrs: { lang: 'en' }, link: [{ rel: 'icon', type: 'image/png', href: '/blahaj.png' }] } },

    css: ['~/assets/css/main.css'],

    runtimeConfig: {
        public: {
            api: { baseUrl: '' },
            appVersion: gitInfo?.semverString ?? gitInfo?.raw ?? 'unknown',
            appCommit: gitInfo?.hash ?? 'unknown',
        },
    },

    compatibilityDate: '2025-01-15',

    vite: { server: { allowedHosts: ['host.docker.internal', 'aspire.dev.internal'] } },

    eslint: { config: { stylistic: { semi: true, arrowParens: true, braceStyle: '1tbs', indent: 4, commaDangle: 'always-multiline' } } },
});
