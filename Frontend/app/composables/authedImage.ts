import type { MaybeRefOrGetter } from 'vue';

/**
 * Fetches an image through the authenticated `$tranga` client (so the Bearer token is sent)
 * and exposes it as an object URL, since a plain `<img src>` can't carry auth headers itself.
 */
export function useAuthedImageUrl(path: MaybeRefOrGetter<string | null | undefined>) {
    const src = ref<string | undefined>(undefined);

    if (import.meta.client) {
        const { $tranga } = useNuxtApp();
        let objectUrl: string | undefined;

        watch(
            () => toValue(path),
            async (p) => {
                if (objectUrl) {
                    URL.revokeObjectURL(objectUrl);
                    objectUrl = undefined;
                }
                if (!p) {
                    src.value = undefined;
                    return;
                }
                try {
                    const blob = await $tranga<Blob>(p, { responseType: 'blob' });
                    objectUrl = URL.createObjectURL(blob);
                    src.value = objectUrl;
                } catch {
                    src.value = undefined;
                }
            },
            { immediate: true },
        );

        onScopeDispose(() => {
            if (objectUrl) URL.revokeObjectURL(objectUrl);
        });
    }

    return src;
}
