<template>
    <UCard>
        {{ extension }}
    </UCard>
</template>

<script setup lang="ts">
import type { ServicesNotificationsNotificationExtension } from '~/api/tranga';

defineProps<{ extension: ServicesNotificationsNotificationExtension }>();
</script>
