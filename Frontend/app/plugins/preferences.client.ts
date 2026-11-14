export default defineNuxtPlugin((nuxtApp) => {
    const mode = useNsfwCoverMode();

    // Wait until hydration is done so the stored value can't cause a server/client mismatch.
    nuxtApp.hook('app:mounted', () => {
        try {
            const stored: string | null = localStorage.getItem(NSFW_COVER_MODE_KEY);
            if (isNsfwCoverMode(stored)) mode.value = stored;
        } catch {
            // localStorage unavailable (private mode, blocked site data) - keep the default.
        }

        watch(mode, (value) => {
            try {
                localStorage.setItem(NSFW_COVER_MODE_KEY, value);
            } catch {
                // Ignore - the preference just won't persist.
            }
        });
    });
});
