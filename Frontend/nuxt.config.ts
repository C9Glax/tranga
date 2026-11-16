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
    modules: ['@nuxt/eslint', '@nuxt/ui', '@nuxtjs/i18n'],

    devtools: { enabled: true },

    app: { head: { title: 'Tranga', htmlAttrs: { lang: 'en' }, link: [{ rel: 'icon', type: 'image/png', href: '/blahaj.png' }] } },

    css: ['~/assets/css/main.css'],

    runtimeConfig: {
        // Populated at runtime from NUXT_SCALAR_DOCS_URL (see Tranga.AppHost/AppHost.cs and
        // https://nuxt.com/docs/4.x/directory-structure/env) - the empty string is just the config-time default.
        scalarDocsUrl: '',
        public: {
            api: { baseUrl: '' },
            appVersion: gitInfo?.semverString ?? gitInfo?.raw ?? 'unknown',
            appCommit: gitInfo?.hash ?? 'unknown',
        },
    },

    compatibilityDate: '2025-01-15',

    vite: { server: { allowedHosts: ['host.docker.internal', 'aspire.dev.internal'] } },

    eslint: { config: { stylistic: { semi: true, arrowParens: true, braceStyle: '1tbs', indent: 4, commaDangle: 'always-multiline' } } },

    // Translations live in i18n/locales/<code>.json and are managed through Weblate (see TRANSLATING.md): only en.json is
    // edited by hand. A new language needs its file plus an entry in `locales` below.
    i18n: {
        strategy: 'no_prefix',
        defaultLocale: 'en',
        // `langDir` is resolved relative to the `i18n/` directory.
        locales: [{ code: 'en', language: 'en-US', name: 'English', file: 'en.json' }],
        langDir: 'locales',
        detectBrowserLanguage: { useCookie: true, cookieKey: 'tranga-locale', fallbackLocale: 'en' },
    },
});
