<template>
    <UFormField :label="label" :description="description" :required="required">
        <div class="flex flex-col gap-1">
            <!-- What an empty value means does not fit into a placeholder, it is shown as tooltip. -->
            <UFieldGroup class="w-full">
                <UTooltip :text="isJson ? $t('customSource.field.jsonPathHint') : selectorHintText">
                    <UInput
                        v-model="selector"
                        :placeholder="isJson ? jsonPlaceholderText : selectorPlaceholderText"
                        :ui="{ base: 'font-mono' }"
                        class="min-w-0 grow" />
                </UTooltip>
                <!-- JSON has no attributes: the path leads to the value itself. -->
                <UTooltip v-if="!isJson" :text="attributeHintText">
                    <UInput v-model="attribute" :placeholder="attributePlaceholderText" :ui="{ base: 'font-mono' }" class="w-36 sm:w-52" />
                </UTooltip>
            </UFieldGroup>

            <!-- Regex and replacement are rarely needed: collapsed unless they are already in use. -->
            <UCollapsible v-model:open="advanced">
                <UButton
                    :label="$t('customSource.field.regexAndReplacement')"
                    icon="i-lucide-regex"
                    :trailing-icon="advanced ? 'i-lucide-chevron-up' : 'i-lucide-chevron-down'"
                    color="neutral"
                    variant="ghost"
                    size="xs"
                    class="self-start" />
                <template #content>
                    <UFieldGroup class="w-full pt-1">
                        <UInput
                            v-model="regex"
                            :placeholder="$t('customSource.field.regex')"
                            :ui="{ base: 'font-mono' }"
                            class="min-w-0 grow" />
                        <UTooltip :text="$t('customSource.field.replacementHint', { example: '/series/{id}/$1' })">
                            <UInput
                                v-model="replacement"
                                :placeholder="$t('customSource.field.replaceWith')"
                                :ui="{ base: 'font-mono' }"
                                class="min-w-0 grow" />
                        </UTooltip>
                    </UFieldGroup>
                </template>
            </UCollapsible>
        </div>
    </UFormField>
</template>

<script setup lang="ts">
const model = defineModel<CustomSourceField>({ required: true });

const props = withDefaults(
    defineProps<{
        label: string;
        description?: string;
        required?: boolean;
        /** What the website answers with: decides whether this is a CSS selector / XPath with an attribute, or a JSON path. */
        format?: CustomSourceFormat;
        selectorPlaceholder?: string;
        attributePlaceholder?: string;
        /** Placeholder of the path input when the format is JSON. */
        jsonPlaceholder?: string;
        /** Tooltip of the selector input: what it means when it is left empty. */
        selectorHint?: string;
        /** Tooltip of the attribute input: what it means when it is left empty. */
        attributeHint?: string;
    }>(),
    { format: 'html' },
);

const { t } = useI18n();

// The texts that are not given by the parent are translated here.
const selectorPlaceholderText = computed(() => props.selectorPlaceholder ?? t('customSource.field.selectorPlaceholder'));
const attributePlaceholderText = computed(() => props.attributePlaceholder ?? t('customSource.field.attributePlaceholder'));
const jsonPlaceholderText = computed(() => props.jsonPlaceholder ?? t('customSource.field.jsonPlaceholder'));
const selectorHintText = computed(() => props.selectorHint ?? t('customSource.field.selectorHint'));
const attributeHintText = computed(() => props.attributeHint ?? t('customSource.field.attributeHint'));

const isJson = computed(() => props.format === 'json');

// Regex and replacement are rarely needed, so they only show up when asked for or already in use.
const advanced = ref(Boolean(model.value.regex || model.value.replacement));
// ... also when a definition that uses them is loaded into the form later (an example, imported JSON).
watch(
    () => Boolean(model.value.regex || model.value.replacement),
    (used) => {
        if (used) advanced.value = true;
    },
);

const bind = (key: keyof CustomSourceField) =>
    computed({
        get: () => model.value[key] ?? '',
        set: (value: string) => {
            model.value = { ...model.value, [key]: value ?? '' };
        },
    });

const selector = bind('selector');
const attribute = bind('attribute');
const regex = bind('regex');
const replacement = bind('replacement');
</script>
