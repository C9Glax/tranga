<template>
    <UPage>
        <UDashboardGroup class="mt-(--ui-header-height)">
            <UDashboardSidebar v-model:collapsed="collapsed" :collapsible="true" :resizable="true">
                <slot name="sidebar">
                    <slot name="pageTitle">
                        <div v-if="pageTitle" class="flex flex-col align-middle my-2">
                            <UIcon
                                v-bind="pageTitle.icon"
                                class="size-10"
                                :class="pageTitle.icon.color && `text-${pageTitle.icon.color}`" />
                            <p class="text-3xl mt-1" :class="collapsed && 'hidden'">{{ pageTitle.title }}</p>
                        </div>
                    </slot>
                    <UInput
                        v-model="searchModel"
                        :disabled="!searchEnabled"
                        placeholder="Search..."
                        :icon="`i-lucide-search${searchEnabled ? '' : '-slash'}`" />
                    <UNavigationMenu :items="nItems" orientation="vertical" />
                </slot>
            </UDashboardSidebar>

            <div class="px-16 py-4 w-full overflow-y-auto">
                <slot />
            </div>
        </UDashboardGroup>
    </UPage>
</template>

<script setup lang="ts">
import type { NavigationMenuItem, NavigationMenuProps } from '@nuxt/ui/components/NavigationMenu.vue';
import type { IconProps } from '@nuxt/ui/components/Icon.vue';
import { LazySearch } from '#components';

export interface TrangaPageProps {
    navigationProps?: NavigationMenuProps;
    pageTitle?: { title: string; icon: IconProps & { color?: string } };
    searchEnabled?: boolean;
}

const searchOverlay = useOverlay().create(LazySearch);

const props = defineProps<TrangaPageProps>();

const collapsed = ref(false);

const nItems = computed((): NavigationMenuItem[][] => {
    const items: NavigationMenuItem[][] = [defaultItems];

    if (props.navigationProps?.items) items.push([...props.navigationProps.items]);

    return items;
});

const defaultItems: NavigationMenuItem[] = [
    { label: 'Tranga', type: 'label' },
    {
        label: 'Back',
        onSelect: () => useRouter().back(),
        icon: 'i-lucide-arrow-left',
        type: 'link',
        ui: { linkLeadingIcon: 'text-secondary', linkLabel: 'text-secondary' },
    },
    { label: 'Home', to: '/', icon: 'i-lucide-home', type: 'link' },
    { label: 'Search Manga', onSelect: () => searchOverlay.open(), icon: 'i-lucide-search' },
    { label: 'Metadata List', to: '/metadata', icon: 'i-lucide-info', type: 'link' },
    { label: 'All Tasks', to: `/tasks`, icon: 'i-lucide-biceps-flexed' },
    { label: 'Downloads', to: `/downloads`, icon: 'i-lucide-cloud-download' },
];

const searchModel = defineModel<string>('search');
</script>
