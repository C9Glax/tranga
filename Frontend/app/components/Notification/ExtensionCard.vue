<template>
    <UCard>
        <div class="flex flex-row items-center justify-between gap-4">
            <div class="flex flex-col">
                <span class="font-medium">{{ extension.name }}</span>
                <span class="text-sm text-muted">{{ extension.type }}</span>
            </div>
            <UButton label="Delete" color="error" loading-auto @click="removeExtension" />
        </div>
    </UCard>
</template>

<script setup lang="ts">
import type { ServicesNotificationsNotificationExtension, DeleteNotificationsExtensionsByExtensionIdResponses } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';

const { extension } = defineProps<{ extension: ServicesNotificationsNotificationExtension }>();

const toast = useToast();
const removeExtension = async () => {
    const result = await useNuxtApp().$tranga<DeleteNotificationsExtensionsByExtensionIdResponses>(
        `/notifications/extensions/${extension.id}`,
        {
            method: 'delete',
            onResponse({ response }) {
                if (response.status !== 200) {
                    toast.add({ title: 'Failed to remove extension.', color: 'error' });
                    return;
                }
                clearNuxtData(ApiKeys.Notifications.Extension(extension.id));
                refreshNuxtData(ApiKeys.Notifications.Extensions);
                toast.add({ title: 'Removed extension.', color: 'success' });
            },
        },
    );
};
</script>
