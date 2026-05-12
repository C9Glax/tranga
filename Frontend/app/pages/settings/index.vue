<template>
    <TrangaPage :page-title="{ title: 'Settings', icon: { name: 'i-lucide-settings' } }">
        <UPageList>
            <UButton to="/settings/notifications" label="Notifications" icon="i-lucide-megaphone" />
        </UPageList>
    </TrangaPage>
</template>
<script setup lang="ts"></script>
