/**
 * Registers a template ref (matching `key`, e.g. `<span ref="add-link">`) as a
 * `useTour` anchor under `key` in the global tour's target registry (see
 * `~/plugins/tour.client.ts`). Vue nulls the template ref automatically when the
 * element unmounts (including on `v-if` toggles), which flows straight through into
 * the registry - no separate cleanup needed for that case.
 */
export function useTourTargetRef(key: string): void {
    const el = useTemplateRef<HTMLElement>(key);
    const { setTourTarget } = useNuxtApp().$tour;

    watch(el, (value) => setTourTarget(key, value ?? null), { immediate: true });
    onUnmounted(() => setTourTarget(key, null));
}
