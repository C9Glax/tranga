/**
 * The display name of a task type. A type without a translation key falls back to its class name split into words.
 */
export const taskTypeLabel = (taskTypeName: string): string => {
    const { t, te } = useNuxtApp().$i18n;
    const key = `tasks.type.${taskTypeName}`;
    // The English file is complete by definition, so checking it (instead of the current language) lets a partially
    // translated language fall back to English for known types.
    return te(key, 'en') ? t(key) : splitCamelCase(taskTypeName);
};
