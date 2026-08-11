<template>
    <UPage>
        <UDashboardGroup class="mt-(--ui-header-height)">
            <UDashboardSidebar v-model:collapsed="collapsed" :collapsible="true" :resizable="true">
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
                </slot>
            </UDashboardSidebar>

            <div class="py-4 w-full overflow-y-auto" :class="!rimless && 'px-16'">
                <slot />
            </div>
        </UDashboardGroup>
    </UPage>
</template>

<script setup lang="ts">
import type { NavigationMenuItem, NavigationMenuProps } from '@nuxt/ui/components/NavigationMenu.vue';
import { LazySearch } from '#components';

const props = defineProps<TrangaPageProps>();

export interface TrangaPageProps {
    navigationProps?: NavigationMenuProps;
    /**
     * If set, search will be enabled and placeholder 'Search <>...'
     */
    searchTerm?: string;
    rimless?: boolean;
}

const searchEnabled = computed(() => props.searchTerm !== undefined);

const searchOverlay = useOverlay().create(LazySearch);

const collapsed = ref(false);

const nItems = computed((): NavigationMenuItem[][] => {
    const items: NavigationMenuItem[][] = [defaultItems.value];

    if (props.navigationProps?.items) items.push([...props.navigationProps.items]);

    return items;
});

const router = useRouter();
const route = useRoute();

const defaultItems = computed((): NavigationMenuItem[] => {
    void route.fullPath;
    const canGoBack = import.meta.client && !!window.history.state?.back;

    return [
        { label: 'Tranga', type: 'label' },
        {
            label: 'Back',
            onSelect: () => router.back(),
            icon: 'i-lucide-arrow-left',
            type: 'link',
            disabled: !canGoBack,
            ui: { linkLeadingIcon: 'text-secondary', linkLabel: 'text-secondary' },
        },
        { label: 'Home', to: '/', icon: 'i-lucide-home', type: 'link' },
        { label: 'Search Manga', onSelect: () => searchOverlay.open(), icon: 'i-lucide-search' },
        { label: 'All Tasks', to: `/tasks`, icon: 'i-lucide-biceps-flexed' },
        { label: 'Workers', to: `/workers`, icon: 'i-lucide-cpu' },
        { label: 'Downloads', to: `/downloads`, icon: 'i-lucide-cloud-download' },
    ];
});

const searchModel = defineModel<string>('search');

const searchInputRef = useTemplateRef('searchInputRef');

defineShortcuts({ ctrl_f: { usingInput: true, handler: () => searchInputRef.value?.inputRef?.focus() } });
</script>
