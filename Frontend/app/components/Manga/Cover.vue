<template>
    <div :class="[widthHeight, 'relative z-0']">
        <div
            v-if="!noBlur"
            class="w-full h-full backdrop-blur-[3px] absolute z-1 bg-linear-[150deg,rgba(245,169,184,0.3)_50%,rgba(91,206,250,0.2)]" />
        <img :src="`${apiBaseUrl}/manga/${mangaId}/cover`" :class="['z-0 absolute', widthHeight]" />
    </div>
</template>

<script setup lang="ts">
const widthHeight = 'w-60 h-90';

const apiBaseUrl = useAppConfig().api.baseUrl;

const props = defineProps<{ mangaId: string; noBlur?: boolean }>();
</script>
