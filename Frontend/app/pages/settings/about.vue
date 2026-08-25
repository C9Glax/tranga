<template>
    <TrangaPage>
        <UPageList>
            <UTable :data="rows" :columns="columns" class="w-full">
                <template #commit-cell="{ row }">
                    <code class="text-sm">{{ row.original.commit }}</code>
                </template>
            </UTable>
        </UPageList>
    </TrangaPage>
</template>

<script setup lang="ts">
import type {
    ServicesMangaGitInfoResponse,
    ServicesTasksGitInfoResponse,
    ServicesNotificationsGitInfoResponse,
    ServicesLibrariesGitInfoResponse,
    ServicesAuthGitInfoResponse,
} from '~/api/tranga';
import type { TableColumn } from '@nuxt/ui/components/Table.vue';
import { ApiKeys } from '~/composables/ApiKeys';

const config = useRuntimeConfig();

const { data: manga } = useTranga<ServicesMangaGitInfoResponse>('/mangas/gitinfo', { key: ApiKeys.GitInfo.Manga });
const { data: tasks } = useTranga<ServicesTasksGitInfoResponse>('/tasks/gitinfo', { key: ApiKeys.GitInfo.Tasks });
const { data: notifications } = useTranga<ServicesNotificationsGitInfoResponse>('/notifications/gitinfo', {
    key: ApiKeys.GitInfo.Notifications,
});
const { data: libraries } = useTranga<ServicesLibrariesGitInfoResponse>('/libraries/gitinfo', { key: ApiKeys.GitInfo.Libraries });
const { data: auth } = useTranga<ServicesAuthGitInfoResponse>('/auth/gitinfo', { key: ApiKeys.GitInfo.Auth });

type Row = { service: string; version: string; commit: string; branch: string };

const toRow = (service: string, gitInfo: { version: string; commit: string; branch: string } | undefined | null): Row => ({
    service,
    version: gitInfo?.version ?? '…',
    commit: gitInfo?.commit ?? '…',
    branch: gitInfo?.branch ?? '…',
});

const rows = computed<Row[]>(() => [
    { service: 'Frontend', version: config.public.appVersion, commit: config.public.appCommit, branch: '' },
    toRow('Manga', manga.value),
    toRow('Tasks', tasks.value),
    toRow('Notifications', notifications.value),
    toRow('Libraries', libraries.value),
    toRow('Auth', auth.value),
]);

const columns: TableColumn<Row>[] = [
    { accessorKey: 'service', header: 'Service' },
    { accessorKey: 'version', header: 'Version' },
    { accessorKey: 'commit', header: 'Commit' },
    { accessorKey: 'branch', header: 'Branch' },
];
</script>
