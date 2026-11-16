<template>
    <UFormField :label="label" :description="description" :required="required">
        <div class="flex flex-col gap-1">
            <!-- What an empty value means does not fit into a placeholder, it is shown as tooltip. -->
            <UFieldGroup class="w-full">
                <UTooltip :text="isJson ? jsonPathHint : selectorHint">
                    <UInput
                        v-model="selector"
                        :placeholder="isJson ? jsonPlaceholder : selectorPlaceholder"
                        :ui="{ base: 'font-mono' }"
                        class="min-w-0 grow" />
                </UTooltip>
                <!-- JSON has no attributes: the path leads to the value itself. -->
                <UTooltip v-if="!isJson" :text="attributeHint">
                    <UInput v-model="attribute" :placeholder="attributePlaceholder" :ui="{ base: 'font-mono' }" class="w-36 sm:w-52" />
                </UTooltip>
            </UFieldGroup>

            <!-- Regex and replacement are rarely needed: collapsed unless they are already in use. -->
            <UCollapsible v-model:open="advanced">
                <UButton
                    label="Regex and replacement"
                    icon="i-lucide-regex"
                    :trailing-icon="advanced ? 'i-lucide-chevron-up' : 'i-lucide-chevron-down'"
                    color="neutral"
                    variant="ghost"
                    size="xs"
                    class="self-start" />
                <template #content>
                    <UFieldGroup class="w-full pt-1">
                        <UInput v-model="regex" placeholder="Regex" :ui="{ base: 'font-mono' }" class="min-w-0 grow" />
                        <UTooltip
                            text="Replaces every match of the regex. $1 is its first group, and the placeholders of this step's URL can be used, e.g. /series/{id}/$1 to build a link from an id.">
                            <UInput v-model="replacement" placeholder="Replace with" :ui="{ base: 'font-mono' }" class="min-w-0 grow" />
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
    {
        format: 'html',
        selectorPlaceholder: 'CSS selector or XPath',
        attributePlaceholder: 'Attribute',
        jsonPlaceholder: 'JSON path',
        selectorHint: 'Leave empty to use the item itself.',
        attributeHint:
            'Leave empty to read the text of the element. Several attributes can be listed, separated by commas: the first one that has a value is used.',
    },
);

const isJson = computed(() => props.format === 'json');
const jsonPathHint =
    'The path to the value inside the item, e.g. title, manga.cover or names[0]. Several paths can be listed, separated by commas: the first one that has a value is used. $ is the item itself.';

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
