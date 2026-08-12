<template>
    <TrangaPage>
        <UPageList>
            <UButton to="/settings/notifications" label="Notifications" icon="i-lucide-megaphone" />
            <UButton to="/settings/libraries" label="Libraries" icon="i-lucide-library" />
            <UButton to="/settings/security" label="Security" icon="i-lucide-key-round" />
            <UButton label="Replay first-download tour" icon="i-lucide-wand-sparkles" variant="outline" @click="replayTour" />
        </UPageList>
    </TrangaPage>
</template>

<script setup lang="ts">
const replayTour = () => useNuxtApp().$tour.restart();
</script>
