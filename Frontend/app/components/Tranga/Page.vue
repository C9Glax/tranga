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
                        <UButton
                            :label="$t('common.logOut')"
                            icon="i-lucide-log-out"
                            color="neutral"
                            variant="ghost"
                            block
                            @click="logout" />
                        <div class="flex gap-1">
                            <UColorModeSelect variant="soft" class="min-w-0 flex-1" />
                            <!-- Only the flag is shown, the sidebar is too narrow for two labelled selects (the list shows the names). -->
                            <ULocaleSelect
                                v-model="selectedLocale"
                                :locales="localeItems"
                                variant="soft"
                                :aria-label="$t('settings.appearance.language.title')"
                                :ui="{ value: 'hidden' }"
                                class="shrink-0" />
                        </div>
                    </div>
                </template>
            </UDashboardSidebar>

            <UDashboardPanel :ui="{ body: rimless && 'p-0 sm:p-0' }">
                <template #header>
                    <UDashboardNavbar :toggle="true">
                        <template #left>
                            <UTooltip :text="$t('common.home')" :kbds="['ctrl', 'h']" class="mx-4 my-2">
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

const { t, locale, locales, setLocale } = useI18n();

// ULocaleSelect wants Nuxt UI's Locale shape, the configured languages only provide a code and a name.
const localeItems = computed(() =>
    locales.value.map((l) => ({
        code: l.code,
        name: l.name ?? l.code,
        dir: l.dir === 'rtl' ? ('rtl' as const) : ('ltr' as const),
        messages: {},
    })),
);

const selectedLocale = computed({ get: () => locale.value, set: (code) => void setLocale(code) });

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
        {
            label: t('common.home'),
            to: '/',
            icon: 'i-lucide-home',
            type: 'link',
            ui: { linkLeadingIcon: 'text-primary', linkLabel: 'text-primary' },
        },
        {
            label: t('common.back'),
            onSelect: () => router.back(),
            icon: 'i-lucide-arrow-left',
            type: 'link',
            disabled: !canGoBack,
            ui: { linkLeadingIcon: 'text-secondary', linkLabel: 'text-secondary' },
        },
        { label: t('nav.tranga'), type: 'label' },
        { label: t('nav.allTasks'), to: `/tasks`, icon: 'i-lucide-biceps-flexed' },
        { label: t('nav.workers'), to: `/workers`, icon: 'i-lucide-cpu' },
        { label: t('nav.sources'), to: '/settings/sources', icon: 'i-lucide-globe' },
        { label: t('nav.settings'), to: '/settings', icon: 'i-lucide-settings' },
        { label: t('nav.links'), type: 'label' },
        {
            label: t('nav.apiDocs'),
            to: '/docs/',
            external: true,
            icon: 'i-lucide-file-code',
            ui: { linkLeadingIcon: 'text-neutral-400', linkLabel: 'text-neutral-400' },
        },
        {
            label: t('nav.github'),
            to: 'https://github.com/C9Glax/tranga',
            external: true,
            icon: 'i-lucide-github',
            ui: { linkLeadingIcon: 'text-neutral-400', linkLabel: 'text-neutral-400' },
        },
        {
            label: t('nav.weblate'),
            to: 'https://hosted.weblate.org/projects/tranga/',
            external: true,
            icon: 'i-simple-icons-weblate',
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
