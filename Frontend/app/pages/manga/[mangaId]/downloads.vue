<template>
    <TrangaPage
        :navigation-props="navigation"
        :page-title="{ title: $t('manga.nav.downloads'), icon: { name: 'i-lucide-cloud-download' } }"
        rimless>
        <UPageSection :ui="{ container: 'px-0 max-w-none sm:py-0 lg:py-0 gap-8 sm:gap-8 mb-8' }">
            <USwitch v-model="includeFinished" :label="$t('downloads.includeFinished')" />
        </UPageSection>
        <div class="w-full h-full p-0 m-0">
            <TasksList :tasks="data" />
        </div>
    </TrangaPage>
</template>

<script setup lang="ts">
import type { GetTasksMangaByMangaIdDownloadsResponse } from '~/api/tranga';
import type { NavigationMenuProps } from '@nuxt/ui/components/NavigationMenu.vue';

const mangaId = useRoute().params.mangaId as string;
const { t } = useI18n();

const includeFinished = useState<boolean>(() => false);
const { data, refresh } = await useTranga<GetTasksMangaByMangaIdDownloadsResponse>(
    () => `/tasks/manga/${mangaId}/downloads?includeFinished=${includeFinished.value}`,
    { lazy: true, watch: [includeFinished] },
);

defineShortcuts({ meta_r: () => refresh() });

let interval: number;
onMounted(() => {
    interval = setInterval(() => refresh(), 5000);
});
onUnmounted(() => clearInterval(interval));

const navigation = computed((): NavigationMenuProps => {
    return {
        items: [
            { label: t('manga.nav.manga'), type: 'label' },
            { label: t('manga.nav.manga'), to: `/manga/${mangaId}`, icon: 'i-lucide-book' },
            { label: t('manga.nav.metadataEntries'), to: `/manga/${mangaId}/metadataEntries`, icon: 'i-lucide-list' },
            { label: t('manga.nav.tasks'), to: `/tasks?manga=${mangaId}`, icon: 'i-lucide-biceps-flexed' },
            { label: t('manga.nav.downloadTasks'), to: `/manga/${mangaId}/downloads`, icon: 'i-lucide-cloud-download' },
            { label: t('common.chapters'), to: `/manga/${mangaId}/chapters`, icon: 'i-lucide-list-checks' },
        ],
    };
});
</script>
