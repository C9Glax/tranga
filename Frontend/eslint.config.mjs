// @ts-check
import withNuxt from './.nuxt/eslint.config.mjs';
import prettierConfig from '@vue/eslint-config-prettier';
import vueI18n from '@intlify/eslint-plugin-vue-i18n';

export default withNuxt(
    prettierConfig,
    // Every user-visible text goes through i18n (see TRANSLATING.md): keys have to exist in en.json, messages have to be
    // valid, and text written directly into a template is reported so that it is not forgotten.
    ...vueI18n.configs['flat/recommended'],
    {
        // Not limited to `files`: the rules that read the locale files (valid-message-syntax, ...) need these settings too.
        settings: { 'vue-i18n': { localeDir: './i18n/locales/*.json', messageSyntaxVersion: '^11.0.0' } },
    },
    {
        files: ['app/**/*.{vue,ts}'],
        ignores: ['app/api/**'],
        rules: {
            '@intlify/vue-i18n/no-raw-text': [
                'error',
                {
                    // Brand names and symbols are not translated. Everything else needs a key.
                    ignorePattern: '^[-#:()&/·|=\\s\\d]*$|^Tranga$|^(Komga|Kavita|Naprise|Gotify|Discord|Telegram|ntfy\\.sh|FlareSolverr)$',
                },
            ],
            '@intlify/vue-i18n/no-missing-keys': 'error',
            // Keys that are put together at runtime (tasks.state.<State>.label, tour.steps.*, ...) cannot be seen by this rule.
            '@intlify/vue-i18n/no-unused-keys': 'off',
            '@intlify/vue-i18n/no-dynamic-keys': 'off',
        },
    },
    {
        // The locale files are formatted by Weblate, not by prettier (see .prettierignore).
        files: ['i18n/locales/*.json'],
        rules: { 'prettier/prettier': 'off' },
    },
);
