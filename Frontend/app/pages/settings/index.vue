<template>
    <TrangaPage>
        <UPageList>
            <UButton to="/settings/notifications" label="Notifications" icon="i-lucide-megaphone" />
            <UButton to="/settings/libraries" label="Libraries" icon="i-lucide-library" />
            <UButton label="Replay Tour" icon="i-lucide-play" variant="outline" @click="replay" />
        </UPageList>
    </TrangaPage>
</template>

<script setup lang="ts">
const { replay } = useAppTour();
</script>
