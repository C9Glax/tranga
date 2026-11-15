<template>
    <TrangaPage>
        <UPageList class="gap-2">
            <UAlert
                v-if="downloadExtensions"
                :title="`${downloadExtensions.length} download source(s) available`"
                description="Tranga searches all of these when looking for a manga to download."
                icon="i-lucide-check"
                color="success"
                variant="subtle" />

            <UPageCard v-for="extension in downloadExtensions" :key="extension.downloadExtensionsId" variant="subtle">
                <div class="flex flex-row items-center gap-3 min-w-0">
                    <img :src="extension.iconUrl" :alt="extension.name" class="size-8 shrink-0 rounded" />
                    <span class="font-medium truncate">{{ extension.name }}</span>
                </div>
            </UPageCard>
        </UPageList>
    </TrangaPage>
</template>

<script setup lang="ts">
import useDownloadExtensions from '~/composables/DownloadExtension';

const { downloadExtensions } = await useDownloadExtensions();
</script>
