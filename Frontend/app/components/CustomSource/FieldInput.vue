<template>
    <UFormField :label="label" :description="description" :required="required">
        <div class="flex flex-row flex-wrap items-center gap-2">
            <UInput v-model="selector" :placeholder="selectorPlaceholder" :ui="{ base: 'font-mono' }" class="min-w-60 grow" />
            <UInput v-model="attribute" :placeholder="attributePlaceholder" :ui="{ base: 'font-mono' }" class="w-44" />
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
    defineProps<{ label: string; description?: string; required?: boolean; selectorPlaceholder?: string; attributePlaceholder?: string }>(),
    { selectorPlaceholder: 'CSS selector or XPath (empty = the item itself)', attributePlaceholder: 'Attribute (empty = text)' },
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
