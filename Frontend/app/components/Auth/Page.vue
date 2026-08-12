<template>
    <div class="flex min-h-screen flex-col items-center justify-center gap-8 p-4">
        <div class="flex items-center gap-2">
            <img src="/blahaj.png" class="h-10" alt="Blahaj" />
            <p
                style="
                    background: linear-gradient(110deg, var(--color-pink), var(--color-blue));
                    background-clip: text;
                    -webkit-background-clip: text;
                    -webkit-text-fill-color: transparent;
                "
                class="text-3xl font-bold">
                Tranga
            </p>
        </div>

        <slot />
    </div>
</template>

<script setup lang="ts"></script>
