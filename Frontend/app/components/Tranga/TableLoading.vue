<template>
    <!-- Shown by UTable (its #loading slot) while the first data is on its way: rows of the same shape as real ones. -->
    <div class="flex flex-col gap-4 py-2" role="status" :aria-label="$t('common.loading')">
        <div v-for="row in rows" :key="row" class="flex items-center gap-4">
            <USkeleton class="size-4 shrink-0" />
            <USkeleton class="h-4 grow" />
            <USkeleton class="hidden h-4 w-1/5 sm:block" />
            <USkeleton class="h-5 w-16 shrink-0 rounded-full" />
        </div>
    </div>
</template>

<script setup lang="ts">
withDefaults(defineProps<{ rows?: number }>(), { rows: 6 });
</script>
