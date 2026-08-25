<template>
    <UModal :title="title" :ui="{ content: 'max-w-md' }">
        <template #content>
            <div class="flex flex-col gap-4 p-6">
                <div class="text-sm text-dimmed">
                    <slot>{{ description }}</slot>
                </div>

                <div class="flex justify-end gap-2">
                    <UButton :label="cancelLabel" variant="ghost" @click="emit('close')" />
                    <UButton :label="confirmLabel" :color="color" :loading="loading" @click="confirm" />
                </div>
            </div>
        </template>
    </UModal>
</template>

<script setup lang="ts">
import type { ButtonProps } from '@nuxt/ui/components/Button.vue';

const props = withDefaults(
    defineProps<{
        title: string;
        description?: string;
        confirmLabel?: string;
        cancelLabel?: string;
        color?: ButtonProps['color'];
        onConfirm: () => Promise<void> | void;
    }>(),
    { confirmLabel: 'Delete', cancelLabel: 'Cancel', color: 'error' },
);
const emit = defineEmits(['close']);

const loading = ref(false);

const confirm = async () => {
    loading.value = true;
    try {
        await props.onConfirm();
    } finally {
        loading.value = false;
        emit('close');
    }
};
</script>
