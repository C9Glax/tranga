<template>
    <TrangaPage>
        <div class="flex flex-col gap-4">
            <h1 class="text-2xl font-semibold">Settings</h1>

            <UTabs v-model="active" :items="items" :content="false" class="w-full" />

            <SettingsNotifications v-if="active === 'notifications'" />
            <SettingsLibraries v-else-if="active === 'libraries'" />
            <SettingsAppearance v-else-if="active === 'appearance'" />
            <SettingsSecurity v-else-if="active === 'security'" />
            <SettingsDebug v-else-if="active === 'debug'" />
        </div>
    </TrangaPage>
</template>

<script setup lang="ts">
import type { TabsItem } from '@nuxt/ui';

// Empty string matches no tab, so nothing is selected until the user picks one.
const active = ref<string>('');

const items: TabsItem[] = [
    { label: 'Notifications', icon: 'i-lucide-megaphone', value: 'notifications' },
    { label: 'Libraries', icon: 'i-lucide-library', value: 'libraries' },
    { label: 'Appearance', icon: 'i-lucide-palette', value: 'appearance' },
    { label: 'Security', icon: 'i-lucide-key-round', value: 'security' },
    { label: 'Debug', icon: 'i-lucide-bug', value: 'debug' },
];
</script>
