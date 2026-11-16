# Translating the Tranga frontend

The frontend uses [`@nuxtjs/i18n`](https://i18n.nuxtjs.org) (vue-i18n). Every text the user sees lives in
`i18n/locales/<language>.json`:

- `en.json` is the **source**. Only developers edit it, by hand.
- All other files are the translations. They are written by [Weblate](https://weblate.org) and are not edited by hand.

Text that comes from the backend (error messages of the API, names and descriptions of extensions) is not part of the
frontend and is not translated here.

## For translators

Translate on Weblate (the link goes here once the project is set up). Things to keep in mind:

- **Placeholders** like `{count}`, `{name}` or `{url}` are replaced by the app. Keep them exactly as they are, but move them
  wherever the sentence needs them. Some of them stand for something that must not be translated, for example `{query}` or
  `{id}` in the custom source texts, which are written the same way in the URL the user types.
- **Plurals** are written as `one | other`, e.g. `{count} chapter | {count} chapters`. Keep the `|` and provide as many
  forms as your language has (separated by `|`).
- Texts for buttons, labels and menu entries should stay short, they have to fit into the interface.
- A text without a translation is shown in English, so partially translated languages work. Do not copy the English
  text to "fill the gaps".

## For developers

Never write text for the user directly into a component. `npm run lint` reports it (`@intlify/vue-i18n/no-raw-text`), and
keys that do not exist in `en.json` (`no-missing-keys`).

```vue
<UButton :label="$t('common.delete')" />
<p>{{ $t('settings.security.created', { date }) }}</p>
```

```ts
const { t } = useI18n();
toast.add({ title: t('toast.chapterDeleted') });

// utils and plugins, where there is no component: the global instance
useNuxtApp().$i18n.t('toast.chapterDeleted');
```

- **Keys are nested** (`area.thing`) and at least two levels deep: a key without a dot is looked up differently by vue-i18n,
  which breaks the fallback to English for blank translations (see `i18n/i18n.config.ts`). Put texts that are used in several
  places into `common.*`, so they are translated once.
- **Never build sentences by concatenation.** Word order differs between languages, use parameters:
  `t('settings.security.created', { date })` with `"Created {date}"`. Markup or components inside a sentence go through
  `<i18n-t keypath="..." tag="p"><template #name>...</template></i18n-t>` (see `Manga/MergeConfirm.vue`).
- **Plurals** use the pipe syntax: `"{count} chapter | {count} chapters"` and `t('key', n)`.
- **Braces, `@` and `|` have a meaning in messages.** To show something like `{id}` literally, pass it in as a parameter:
  `t('key', { id: '{id}' })` with `"... {id} ..."`.
- **Code is not text**: selectors, regular expressions, URLs, file formats and brand names (Komga, Kavita, ...) are written
  directly in the component (they are not translated).
- **Dynamic keys** (``t(`tasks.state.${state}.label`)``) are fine, but then nothing checks that every key exists: add all
  variants to `en.json`.
- **Dates and relative times** are formatted with the current language (`useI18n().locale`), see `utils/getRelativeTime.ts`
  and `components/Tranga/Time.vue`.
- Changing or removing the text of a key that already has translations: use a **new key** when the meaning changes, so the old
  translations are not shown for a different sentence. Weblate marks translations of a changed source text as needing review.

### Adding a language

Weblate adds `i18n/locales/<code>.json` (the code is the BCP 47 tag, e.g. `de`, `pt-BR`) in a pull request. To make the
language selectable, add it to `i18n.locales` in `nuxt.config.ts`:

```ts
{ code: 'de', language: 'de-DE', name: 'Deutsch', file: 'de.json' },
```

`name` is the language written in itself (so it can be found even when the current language is not readable), `language` is
used for `<html lang>` and for formatting dates. [Nuxt UI](https://ui.nuxt.com/docs/getting-started/integrations/i18n/nuxt)
has translations of its own components (pagination, date pickers, ...) for most languages. They are picked automatically by
code (`pt-BR` becomes `pt_br`), and fall back to English.

Only merge a new language once a reasonable part of it is translated: it is listed in the language picker as soon as it is
in `nuxt.config.ts`.
