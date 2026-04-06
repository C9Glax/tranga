<template>
    <UPage>
        <UDashboardGroup class="mt-(--ui-header-height)">
            <UDashboardSidebar v-model:collapsed="collapsed" :collapsible="true" :resizable="true">
                <slot name="sidebar">
                    <slot name="pageTitle">
                        <div v-if="pageTitle" class="flex flex-row gap-2 align-middle my-2">
                            <UIcon
                                v-bind="pageTitle.icon"
                                class="size-10"
                                :class="pageTitle.icon.color && `text-${pageTitle.icon.color}`" />
                            <p class="text-3xl mt-1" :class="collapsed && 'hidden'">{{ pageTitle.title }}</p>
                        </div>
                    </slot>
                    <UNavigationMenu :items="nItems" orientation="vertical" />
                </slot>
            </UDashboardSidebar>

            <div class="p-16 w-full overflow-y-auto">
                <slot />
            </div>
        </UDashboardGroup>
    </UPage>
</template>

<script setup lang="ts">
import type { NavigationMenuItem, NavigationMenuProps } from '@nuxt/ui/components/NavigationMenu.vue';
import type { IconProps } from '@nuxt/ui/components/Icon.vue';

export interface TrangaPageProps {
    navigationProps?: NavigationMenuProps;
    pageTitle?: { title: string; icon: IconProps & { color?: string } };
}

const props = defineProps<TrangaPageProps>();

const collapsed = ref(false);

const nItems = computed((): NavigationMenuItem[][] => {
    const items: NavigationMenuItem[] = defaultItems;

    if (props.navigationProps?.items) items.push([...props.navigationProps.items]);

    return [items];
});

const defaultItems: NavigationMenuItem[] = [
    { label: 'Home', to: '/', icon: 'i-lucide-home', type: 'link', ui: { linkLeadingIcon: 'text-primary', linkLabel: 'text-primary' } },
    {
        label: 'Back',
        onSelect: () => useRouter().back(),
        icon: 'i-lucide-arrow-left',
        type: 'link',
        ui: { linkLeadingIcon: 'text-secondary', linkLabel: 'text-secondary' },
    },
];
</script>
