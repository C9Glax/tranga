<template>
    <UFormField :label="label" :description="description" :required="required">
        <div class="flex flex-col gap-1">
            <!-- What an empty value means does not fit into a placeholder, it is shown as tooltip. -->
            <UFieldGroup class="w-full">
                <UTooltip :text="selectorHint">
                    <UInput v-model="selector" :placeholder="selectorPlaceholder" :ui="{ base: 'font-mono' }" class="min-w-0 grow" />
                </UTooltip>
                <UTooltip :text="attributeHint">
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
                        <UInput v-model="replacement" placeholder="Replace with" :ui="{ base: 'font-mono' }" class="min-w-0 grow" />
                    </UFieldGroup>
                </template>
            </UCollapsible>
        </div>
    </UFormField>
</template>

<script setup lang="ts">
const model = defineModel<CustomSourceField>({ required: true });

withDefaults(
    defineProps<{
        label: string;
        description?: string;
        required?: boolean;
        selectorPlaceholder?: string;
        attributePlaceholder?: string;
        /** Tooltip of the selector input: what it means when it is left empty. */
        selectorHint?: string;
        /** Tooltip of the attribute input: what it means when it is left empty. */
        attributeHint?: string;
    }>(),
    {
        selectorPlaceholder: 'CSS selector or XPath',
        attributePlaceholder: 'Attribute',
        selectorHint: 'Leave empty to use the item itself.',
        attributeHint:
            'Leave empty to read the text of the element. Several attributes can be listed, separated by commas: the first one that has a value is used.',
    },
);

// Regex and replacement are rarely needed, so they only show up when asked for or already in use.
const advanced = ref(Boolean(model.value.regex || model.value.replacement));

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
