<template>
    <UFormField :label="label" :description="description" :required="required">
        <div class="flex flex-row flex-wrap items-center gap-2">
            <!-- What an empty value means does not fit into a placeholder, it is shown as tooltip. -->
            <div class="min-w-60 grow">
                <UTooltip :text="selectorHint">
                    <UInput v-model="selector" :placeholder="selectorPlaceholder" :ui="{ base: 'font-mono' }" class="w-full" />
                </UTooltip>
            </div>
            <UTooltip :text="attributeHint">
                <UInput v-model="attribute" :placeholder="attributePlaceholder" :ui="{ base: 'font-mono' }" class="w-52" />
            </UTooltip>
            <template v-if="advanced">
                <UInput v-model="regex" placeholder="Regex" :ui="{ base: 'font-mono' }" class="w-48" />
                <UInput v-model="replacement" placeholder="Replace with" :ui="{ base: 'font-mono' }" class="w-40" />
            </template>
            <UTooltip v-else text="Post-process the value with a regular expression">
                <UButton icon="i-lucide-regex" color="neutral" variant="ghost" size="sm" aria-label="Add regex" @click="advanced = true" />
            </UTooltip>
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
