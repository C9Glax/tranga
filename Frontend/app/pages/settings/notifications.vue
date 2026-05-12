<template>
    <TrangaPage :page-title="{ title: 'Notifications', icon: { name: 'i-lucide-megaphone' } }">
        <UPageList class="gap-2">
            <UCollapsible :open="open">
                <UButton
                    label="Add extension"
                    color="neutral"
                    variant="subtle"
                    trailing-icon="i-lucide-chevron-down"
                    @click="open = !open"
                    block />

                <template #content>
                    <UCard class="m-0.5 mt-2">
                        <div class="flex flex-col gap-4">
                            <UTabs v-model="active" :items="items" orientation="vertical" value-key="slot" class="h-62">
                                <template #naprise>
                                    <UForm :state="state" class="flex flex-col gap-4">
                                        <UFormField label="Name" name="name">
                                            <UInput v-model="state.name" class="w-full" />
                                        </UFormField>
                                        <UFormField label="Service Url" name="serviceUrl">
                                            <UInput v-model="state.serviceUrl" class="w-full" />
                                        </UFormField>
                                    </UForm>
                                </template>

                                <template #gotify>
                                    <UForm :state="state" class="flex flex-col gap-4">
                                        <UFormField label="Name" name="name">
                                            <UInput v-model="state.name" class="w-full" />
                                        </UFormField>
                                        <div class="flex flex-row gap-2">
                                            <UFormField label="Host" name="host" class="grow">
                                                <UInput v-model="state.host" class="w-full" @update:modelValue="autoPort" />
                                            </UFormField>
                                            <UFormField label="Port" name="port">
                                                <UInput v-model="state.port" class="w-full" type="number" />
                                            </UFormField>
                                        </div>
                                        <UFormField label="AppToken" name="appToken">
                                            <UInput v-model="state.appToken" class="w-full" type="password" />
                                        </UFormField>
                                    </UForm>
                                </template>

                                <template #discord>
                                    <UForm :state="state" class="flex flex-col gap-4">
                                        <UFormField label="Name" name="name">
                                            <UInput v-model="state.name" class="w-full" />
                                        </UFormField>
                                        <UFormField label="Webhook ID" name="webhookId">
                                            <UInput v-model="state.webhookId" class="w-full" />
                                        </UFormField>
                                        <UFormField label="Webhook Token" name="webhookToken">
                                            <UInput v-model="state.webhookToken" class="w-full" />
                                        </UFormField>
                                    </UForm>
                                </template>

                                <template #ntfysh>
                                    <UForm :state="state" class="flex flex-col gap-4">
                                        <UFormField label="Name" name="name">
                                            <UInput v-model="state.name" class="w-full" />
                                        </UFormField>
                                        <div class="flex flex-row gap-2">
                                            <UFormField label="Host" name="host" class="grow">
                                                <UInput v-model="state.host" class="w-full" @update:modelValue="autoPort" />
                                            </UFormField>
                                            <UFormField label="Port" name="port">
                                                <UInput v-model="state.port" class="w-full" type="number" />
                                            </UFormField>
                                        </div>
                                        <div class="flex flex-row gap-2">
                                            <UFormField label="Username" name="user" class="grow">
                                                <UInput v-model="state.user as string | undefined" class="w-full" />
                                            </UFormField>
                                            <UFormField label="Password" name="password" class="grow">
                                                <UInput v-model="state.password as string | undefined" class="w-full" type="password" />
                                            </UFormField>
                                        </div>
                                        <UFormField label="Topic" name="topic">
                                            <UInput v-model="state.topic" class="w-full" />
                                        </UFormField>
                                    </UForm>
                                </template>

                                <template #telegram>
                                    <UForm :state="state" class="flex flex-col gap-4">
                                        <UFormField label="Name" name="name">
                                            <UInput v-model="state.name" class="w-full" />
                                        </UFormField>
                                        <UFormField label="Chat ID" name="chatId">
                                            <UInput v-model="state.chatId" class="w-full" />
                                        </UFormField>
                                        <UFormField label="Token" name="token">
                                            <UInput v-model="state.token" class="w-full" />
                                        </UFormField>
                                    </UForm>
                                </template>
                            </UTabs>
                            <div class="flex flex-row gap-4 justify-end">
                                <UButton
                                    label="Cancel"
                                    @click="
                                        () => {
                                            state = {};
                                            open = false;
                                        }
                                    "
                                    color="secondary"
                                    variant="soft" />
                                <UButton label="Add" @click="createExtension" loading-auto icon="i-lucide-plus" />
                            </div>
                        </div>
                    </UCard>
                </template>
            </UCollapsible>
            <NotificationExtensionCard v-for="extension in extensions" :key="extension.id" :extension="extension" />
        </UPageList>
    </TrangaPage>
</template>

<script setup lang="ts">
import type {
    GetNotificationsExtensionsResponse,
    ServicesNotificationsPutExtensionRequestNapriseServiceUrl,
    ServicesNotificationsPutExtensionRequestGotify,
    ServicesNotificationsPutExtensionRequestDiscord,
    ServicesNotificationsPutExtensionRequestNtfySh,
    ServicesNotificationsPutExtensionRequestTelegram,
    PutNotificationsExtensionsNapriseResponses,
    PutNotificationsExtensionsGotifyResponses,
    PutNotificationsExtensionsDiscordResponses,
    PutNotificationsExtensionsNtfyshResponses,
    PutNotificationsExtensionsTelegramResponses,
} from '~/api/tranga';
import { ApiKeys } from '~/composables/ApiKeys';
import Notifications = ApiKeys.Notifications;

const toast = useToast();

const open = ref(false);
const active = ref<string>('naprise');
const items = [
    { label: 'Naprise', slot: 'naprise' },
    { label: 'Gotify', slot: 'gotify' },
    { label: 'Discord', slot: 'discord' },
    { label: 'ntfy.sh', slot: 'ntfysh' },
    { label: 'Telegram', slot: 'telegram' },
];

const { data: extensions } = useTranga<GetNotificationsExtensionsResponse>('/notifications/extensions', { key: Notifications.Extensions });

const state = ref<
    Partial<
        ServicesNotificationsPutExtensionRequestNapriseServiceUrl &
            ServicesNotificationsPutExtensionRequestGotify &
            ServicesNotificationsPutExtensionRequestDiscord &
            ServicesNotificationsPutExtensionRequestNtfySh &
            ServicesNotificationsPutExtensionRequestTelegram
    >
>({});

const autoPort = () => {
    if (state.value.host?.startsWith('https:') && !state.value.port) state.value.port = 443;
    else if (state.value.host?.startsWith('http:') && !state.value.port) state.value.port = 80;
};

const createExtension = async () => {
    let result:
        | (
              | PutNotificationsExtensionsNapriseResponses
              | PutNotificationsExtensionsGotifyResponses
              | PutNotificationsExtensionsDiscordResponses
              | PutNotificationsExtensionsNtfyshResponses
              | PutNotificationsExtensionsTelegramResponses
          )
        | undefined = undefined;
    switch (active.value) {
        case 'naprise':
            result = await useNuxtApp().$tranga<PutNotificationsExtensionsNapriseResponses>('/notifications/extensions/naprise', {
                method: 'put',
                body: state.value,
            });
            break;
        case 'gotify':
            result = await useNuxtApp().$tranga<PutNotificationsExtensionsGotifyResponses>('/notifications/extensions/gotify', {
                method: 'put',
                body: state.value,
            });
            break;
        case 'discord':
            result = await useNuxtApp().$tranga<PutNotificationsExtensionsDiscordResponses>('/notifications/extensions/discord', {
                method: 'put',
                body: state.value,
            });
            break;
        case 'ntfysh':
            result = await useNuxtApp().$tranga<PutNotificationsExtensionsNtfyshResponses>('/notifications/extensions/ntfysh', {
                method: 'put',
                body: state.value,
            });
            break;
        case 'telegram':
            result = await useNuxtApp().$tranga<PutNotificationsExtensionsTelegramResponses>('/notifications/extensions/telegram', {
                method: 'put',
                body: state.value,
            });
            break;
    }
    if (!result?.['200']) {
        toast.add({ title: 'Failed adding extension!', color: 'error' });
        return;
    }

    toast.add({ title: 'Added extension.', color: 'success' });
    state.value = {};
    open.value = false;
};
</script>
