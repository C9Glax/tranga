<template>
    <UCard>
        <div class="flex flex-row items-center justify-between gap-4">
            <div class="flex flex-col">
                <span class="font-medium">{{ library.libraryServiceType }}</span>
                <span class="text-sm text-muted">{{ library.baseUrl }}</span>
            </div>
            <UButton label="Delete" color="error" @click="removeLibrary" loading-auto />
        </div>
    </UCard>
</template>

<script setup lang="ts">
import type { ServicesLibrariesLibrary, DeleteLibrariesByLibraryIdResponses } from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';

const { library } = defineProps<{ library: ServicesLibrariesLibrary }>();

const toast = useToast();
const removeLibrary = async () => {
    await useNuxtApp().$tranga<DeleteLibrariesByLibraryIdResponses>(`/libraries/${library.id}`, {
        method: 'delete',
        onResponse({ response }) {
            if (response.status !== 200) {
                toast.add({ title: 'Failed to remove library.', color: 'error' });
                return;
            }
            clearNuxtData(ApiKeys.Libraries.Library(library.id));
            refreshNuxtData(ApiKeys.Libraries.Libraries);
            toast.add({ title: 'Removed library.', color: 'success' });
        },
    });
};
</script>
