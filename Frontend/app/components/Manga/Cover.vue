<template>
    <div v-if="src" class="relative overflow-clip aspect-6/9" :class="{ 'cursor-pointer': revealable }" @click="onClick">
        <img
            :src="src"
            class="size-full object-cover object-center transition duration-300"
            :class="blurred ? 'blur-xl scale-110' : legacyBlur && 'blur-md'" />
        <div v-if="blurred" class="absolute inset-0 flex items-center justify-center bg-black/20">
            <UIcon :name="revealable ? 'i-lucide-eye' : 'i-lucide-eye-off'" class="size-8 max-h-full text-white drop-shadow" />
        </div>
    </div>
    <USkeleton v-else class="aspect-6/9" />
</template>

<script setup lang="ts">
// Passing `nsfw` (even as false) opts into the NSFW cover preference (see `useNsfwCoverMode`);
// without it the legacy `noBlur` behaviour applies.
const props = defineProps<{ mangaId?: string | null; fileId?: string | null; noBlur?: boolean; nsfw?: boolean | null; size: CoverSize }>();

const path = computed(() => {
    let base: string | undefined;
    if (props.fileId) base = `/mangas/files/${props.fileId}`;
    else if (props.mangaId) base = `/mangas/${props.mangaId}/cover`;
    return base ? withCoverSize(base, props.size) : undefined;
});

const src = useAuthedImageUrl(path);

const mode = useNsfwCoverMode();
const revealed = ref(false);

const legacyBlur = computed(() => props.nsfw === undefined && !props.noBlur);
const blurred = computed(() => {
    if (!props.nsfw) return false;
    if (mode.value === 'show') return false;
    if (mode.value === 'click') return !revealed.value;
    return true;
});
const revealable = computed(() => !!props.nsfw && mode.value === 'click' && !revealed.value);

const onClick = (event: MouseEvent) => {
    if (!revealable.value) return;
    // Don't follow a surrounding link on the click that reveals the cover.
    event.preventDefault();
    event.stopPropagation();
    revealed.value = true;
};
</script>
