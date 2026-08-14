<template>
    <UPage>
        <UDashboardGroup>
            <UDashboardSidebar :default-size="20" :min-size="20" :max-size="20">
                <template #default>
                    <div class="h-(--ui-header-height)" />
                    <UNavigationMenu :items="nItems" orientation="vertical" />
                </template>
                <template #footer>
                    <div class="flex w-full flex-col gap-1">
                        <UButton label="Log out" icon="i-lucide-log-out" color="neutral" variant="ghost" block @click="logout" />
                        <UColorModeSelect variant="soft" class="w-full" />
                    </div>
                </template>
            </UDashboardSidebar>

            <UDashboardPanel :ui="{ body: rimless && 'p-0 sm:p-0' }">
                <template #header>
                    <UDashboardNavbar :toggle="true">
                        <template #left>
                            <UTooltip text="Home" :kbds="['ctrl', 'h']" class="mx-4 my-2">
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
                            <span ref="search-button">
                                <UDashboardSearchButton />
                            </span>
                        </template>
                    </UDashboardNavbar>
                </template>
                <template #body>
                    <slot />
                </template>
            </UDashboardPanel>

            <MangaSearch />
        </UDashboardGroup>
    </UPage>
</template>

<script setup lang="ts">
import type { NavigationMenuItem } from '@nuxt/ui/components/NavigationMenu.vue';
import { clearAuthToken } from '~/composables/authToken';

useTourTargetRef('search-button');

const props = defineProps<TrangaPageProps>();

export interface TrangaPageProps {
    /**
     * Additional section in the navigation menu
     */
    navigation?: TrangaPageTitleProps;
    rimless?: boolean;
}

export interface TrangaPageTitleProps {
    title: NavigationMenuItem;
    items: NavigationMenuItem[];
}

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
        { label: 'Home', to: '/', icon: 'i-lucide-home', type: 'link', ui: { linkLeadingIcon: 'text-primary', linkLabel: 'text-primary' } },
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
        { label: 'Sources', to: '/settings/sources', icon: 'i-lucide-globe', class: 'tour-sources-nav' },
        { label: 'Settings', to: '/settings', icon: 'i-lucide-settings' },
        { label: 'Links', type: 'label' },
        {
            label: 'Github',
            to: 'https://github.com/C9Glax/tranga',
            external: true,
            icon: 'i-lucide-github',
            ui: { linkLeadingIcon: 'text-neutral-400', linkLabel: 'text-neutral-400' },
        },
    ];
});

defineShortcuts({ ctrl_h: () => navigateTo('/') });

const logout = async () => {
    clearAuthToken();
    await navigateTo('/login');
};
</script>
