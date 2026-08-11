<template>
    <UPage>
        <UDashboardGroup>
            <UDashboardSidebar resizable>
                <template #default>
                    <slot name="sidebar">
                        <UTooltip text="Search" :kbds="['ctrl', 'f']">
                            <UInput
                                ref="searchInputRef"
                                v-model="searchModel"
                                :disabled="!searchEnabled"
                                :placeholder="`Search ${searchTerm ?? ''}...`"
                                :icon="`i-lucide-search${searchEnabled ? '' : '-slash'}`" />
                        </UTooltip>
                        <UNavigationMenu :items="nItems" orientation="vertical" />
                        <UColorModeSelect variant="soft" :ui="{ base: 'place-self-end' }" />
                    </slot>
                </template>
            </UDashboardSidebar>

            <UDashboardPanel resizable>
                <template #header>
                    <UDashboardNavbar :toggle="true">
                        <template #left>
                            <UTooltip text="Home" :kbds="['ctrl', 'h']">
                                <NuxtLink to="/">
                                    <div class="h-full flex gap-2 items-center">
                                        <img src="/blahaj.png" class="h-lh cursor-grab" alt="Blahaj" />
                                        <p
                                            style="
                                                background: linear-gradient(110deg, var(--color-pink), var(--color-blue));
                                                background-clip: text;
                                                -webkit-background-clip: text;
                                                -webkit-text-fill-color: transparent;
                                            "
                                            class="font-bold cursor-pointer text-3xl">
                                            Tranga
                                        </p>
                                    </div>
                                </NuxtLink>
                            </UTooltip>
                        </template>
                        <template #right>
                            <UTooltip :kbds="['ctrl', 's']">
                                <UButton icon="i-lucide-book-search" label="Search" @click="searchOverlay.open()" />
                            </UTooltip>
                        </template>
                    </UDashboardNavbar>
                </template>
                <template #body>
                    <slot />
                </template>
            </UDashboardPanel>
        </UDashboardGroup>
    </UPage>
</template>

<script setup lang="ts">
import type { NavigationMenuItem } from '@nuxt/ui/components/NavigationMenu.vue';
import { LazySearch } from '#components';

const props = defineProps<TrangaPageProps>();

export interface TrangaPageProps {
    /**
     * Additional section in the navigation menu
     */
    navigation?: { title: NavigationMenuItem; items: NavigationMenuItem[] };
    /**
     * If set, search will be enabled and placeholder 'Search <>...'
     */
    searchTerm?: string;
    rimless?: boolean;
}

const searchEnabled = computed(() => props.searchTerm !== undefined);

const searchOverlay = useOverlay().create(LazySearch);

/**
 * The items for the navigation-menu
 */
const nItems = computed((): NavigationMenuItem[][] => {
    const items: NavigationMenuItem[][] = [defaultItems.value];

    if (props.navigation) {
        items.push([props.navigation.title, ...props.navigation.items]);
    }

    return items;
});

const router = useRouter();
const route = useRoute();

const defaultItems = computed((): NavigationMenuItem[] => {
    void route.fullPath;
    const canGoBack = import.meta.client && !!window.history.state?.back;

    return [
        { label: 'Home', to: '/', icon: 'i-lucide-home', type: 'link' },
        {
            label: 'Back',
            onSelect: () => router.back(),
            icon: 'i-lucide-arrow-left',
            type: 'link',
            disabled: !canGoBack,
            ui: { linkLeadingIcon: 'text-secondary', linkLabel: 'text-secondary' },
        },
        { label: 'Tranga', type: 'label' },
        { label: 'All Tasks', to: `/tasks`, icon: 'i-lucide-biceps-flexed' },
        { label: 'Workers', to: `/workers`, icon: 'i-lucide-cpu' },
        { label: 'Downloads', to: `/downloads`, icon: 'i-lucide-cloud-download' },
        { label: 'Settings', to: '/settings', icon: 'i-lucide-settings' },
        { label: 'Links', type: 'label' },
        { label: 'Github', to: 'https://github.com/C9Glax/tranga', external: true, icon: 'i-lucide-github' },
    ];
});

const searchModel = defineModel<string>('search');

const searchInputRef = useTemplateRef('searchInputRef');

defineShortcuts({
    ctrl_s: () => searchOverlay.open(),
    ctrl_h: () => navigateTo('/'),
    ctrl_f: { usingInput: true, handler: () => searchInputRef.value?.inputRef?.focus() },
});
</script>
