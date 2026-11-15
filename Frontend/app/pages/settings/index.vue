<template>
    <TrangaPage>
        <div class="flex flex-col gap-4">
            <h1 class="text-2xl font-semibold">Settings</h1>

            <div class="flex flex-col gap-2 md:flex-row">
                <UTabs
                    v-model="active"
                    :items="items"
                    :content="false"
                    orientation="vertical"
                    :ui="{ trigger: 'w-full justify-start text-left' }"
                    class="shrink-0 self-start md:sticky md:top-0 md:w-48" />

                <div class="min-w-0 flex-1">
                    <SettingsNotifications v-if="active === 'notifications'" />
                    <SettingsLibraries v-else-if="active === 'libraries'" />
                    <SettingsAppearance v-else-if="active === 'appearance'" />
                    <SettingsBehavior v-else-if="active === 'behavior'" />
                    <SettingsSecurity v-else-if="active === 'security'" />
                    <SettingsDebug v-else-if="active === 'debug'" />
                </div>
            </div>
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
    { label: 'Behavior', icon: 'i-lucide-sliders-horizontal', value: 'behavior' },
    { label: 'Security', icon: 'i-lucide-key-round', value: 'security' },
    { label: 'Debug', icon: 'i-lucide-bug', value: 'debug' },
];
</script>
