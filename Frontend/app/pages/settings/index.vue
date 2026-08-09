<template>
    <TrangaPage :page-title="{ title: 'Settings', icon: { name: 'i-lucide-settings' } }">
        <UPageList>
            <UButton to="/settings/notifications" label="Notifications" icon="i-lucide-megaphone" />
            <UButton to="/settings/libraries" label="Libraries" icon="i-lucide-library" />
        </UPageList>
    </TrangaPage>
</template>
<script setup lang="ts"></script>
