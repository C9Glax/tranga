import type { MessageResolver, PathValue } from 'vue-i18n';

/**
 * Looks a message up by its dotted path (`settings.appearance.language.title`) in the nested message object.
 *
 * Unlike the default resolver, a blank message ("") counts as missing. Weblate writes strings that are not translated yet
 * as empty strings, and vue-i18n would show those as nothing at all instead of falling back to English. (Only keys with
 * at least one dot are covered: vue-i18n looks a key up directly in the messages once the resolver found nothing, so all
 * keys have to be nested.)
 */
const resolveMessage: MessageResolver = (messages: unknown, path: string): PathValue => {
    let value: unknown = messages;
    for (const part of path.split('.')) {
        if (value === null || typeof value !== 'object') return null;
        value = (value as Record<string, unknown>)[part];
    }
    if (typeof value === 'string') return value === '' ? null : value;
    return typeof value === 'object' ? (value as PathValue) : null;
};

export default defineI18nConfig(() => ({ legacy: false, fallbackLocale: 'en', messageResolver: resolveMessage }));
