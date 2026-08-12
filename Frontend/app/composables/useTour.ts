import type { MaybeRefOrGetter, Ref, ComputedRef } from 'vue';

export interface TourStep {
    /**
     * The element this step points to. Accepts a CSS selector (`'#id'`, `'.class'`),
     * an element, or a ref/getter returning either. Omit or pass `null` to anchor
     * the step to the center of the viewport (used for intro/summary steps).
     */
    target?: MaybeRefOrGetter<string | HTMLElement | null | undefined>;
    /** Any other fields (title, body, key, …) are passed through untouched. */
    [key: string]: unknown;
}

export interface UseTourOptions {
    /** The step index the tour starts on. @defaultValue 0 */
    initialStep?: number;
    /** Loop back to the first step after the last one. @defaultValue false */
    loop?: boolean;
    /** Scroll the target into view when a step becomes active. @defaultValue true */
    scrollIntoView?: boolean | ScrollIntoViewOptions;
}

export interface UseTourReturn {
    open: Ref<boolean>;
    index: Ref<number>;
    current: ComputedRef<TourStep | undefined>;
    reference: ComputedRef<HTMLElement | { getBoundingClientRect: () => DOMRect } | undefined>;
    total: ComputedRef<number>;
    hasNext: ComputedRef<boolean>;
    hasPrev: ComputedRef<boolean>;
    start: (index?: number) => void;
    next: () => void;
    prev: () => void;
    goTo: (index: number) => void;
    finish: () => void;
}

/**
 * Local re-implementation of Nuxt UI's `useTour` composable (added in @nuxt/ui
 * 4.9.0). Both 4.9.0 and 4.10.0 - the only published versions that ship it - have a
 * regression that breaks the `DashboardSearch` command palette's input (verified
 * directly: reverting to 4.6.0 fixes it, reproduces on both 4.9.0 and 4.10.0
 * regardless of whether the tour is even active). Vendoring the same small,
 * well-defined composable lets the rest of the app stay on the known-good 4.6.0
 * release instead of trading a working search bar for the tour feature.
 *
 * Behavior mirrors upstream exactly: a single popover re-anchors to each step's
 * `target` as `index` changes, `target` may be a plain value or a reactive
 * getter/ref (Vue's own dependency tracking makes `reference` re-resolve once a
 * getter's underlying ref/reactive read changes, e.g. once a page mounts).
 */
export function useTour(steps: MaybeRefOrGetter<TourStep[]>, options: UseTourOptions = {}): UseTourReturn {
    const { loop = false, scrollIntoView = true } = options;

    const stepList = computed(() => toValue(steps) ?? []);
    const total = computed(() => stepList.value.length);
    const open = ref(false);
    const rawIndex = ref(options.initialStep ?? 0);

    const clamp = (value: number) => Math.min(Math.max(value, 0), Math.max(total.value - 1, 0));

    const index = computed({ get: () => clamp(rawIndex.value), set: (value: number) => (rawIndex.value = clamp(value)) });

    const current = computed(() => stepList.value[index.value]);
    const hasNext = computed(() => index.value < total.value - 1);
    const hasPrev = computed(() => index.value > 0);

    const centerAnchor = {
        getBoundingClientRect: (): DOMRect => {
            const x = typeof window === 'undefined' ? 0 : window.innerWidth / 2;
            const y = typeof window === 'undefined' ? 0 : window.innerHeight / 2;
            return { x, y, top: y, left: x, right: x, bottom: y, width: 0, height: 0, toJSON: () => ({}) } as DOMRect;
        },
    };

    const reference = computed(() => {
        if (!open.value || typeof window === 'undefined') return undefined;
        const target = toValue(current.value?.target as MaybeRefOrGetter<string | HTMLElement | null | undefined>);
        if (target == null) return centerAnchor;
        if (typeof target === 'string') {
            const selector = target.startsWith('#') || target.startsWith('.') ? target : `#${target}`;
            try {
                return document.querySelector<HTMLElement>(selector) ?? undefined;
            } catch {
                return undefined;
            }
        }
        return target;
    });

    const scrollTargetIntoView = (): void => {
        if (!scrollIntoView) return;
        const el = reference.value;
        if (el instanceof Element) {
            el.scrollIntoView(typeof scrollIntoView === 'object' ? scrollIntoView : { behavior: 'smooth', block: 'center' });
        }
    };

    const goTo = (value: number): void => {
        index.value = value;
        if (total.value > 0) open.value = true;
    };

    const start = (value: number = options.initialStep ?? 0): void => goTo(value);

    const finish = (): void => {
        open.value = false;
    };

    const next = (): void => {
        if (hasNext.value) index.value++;
        else if (loop) index.value = 0;
        else finish();
    };

    const prev = (): void => {
        if (hasPrev.value) index.value--;
    };

    watch(total, (value) => {
        if (!value) open.value = false;
    });

    watch([open, index], () => {
        if (open.value) nextTick(scrollTargetIntoView);
    });

    return { open, index, current, reference, total, hasNext, hasPrev, start, next, prev, goTo, finish };
}
