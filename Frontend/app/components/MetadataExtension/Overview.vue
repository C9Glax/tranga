<template>
    <UPageCard>
        <MetadataExtensionIcon :id="metadataLink.metadataExtensionId" />
    </UPageCard>
</template>

<script setup lang="ts">
defineProps<{ metadataLink: { metadataExtensionId: string } }>();
</script>
