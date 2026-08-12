import type { TourStep, UseTourReturn } from '~/composables/useTour';

const SEEN_KEY = 'tranga-tour-first-download-seen';

const TOUR_KEYS = ['search-button', 'use-as-source', 'go-to-manga', 'more-download-links', 'add-link', 'match-download-link'] as const;
type TourKey = (typeof TOUR_KEYS)[number];

/**
 * "Use as Source for Manga", "Go to Manga" and "More Download-Links" only exist as
 * entries inside `ButtonProps[]`/`NavigationMenuItem[]` arrays passed into shared
 * page components (see `Metadata/Page.vue`/`Manga/Page.vue`'s `actions` prop) - a Vue
 * ref can't attach inside a plain data array. "Match" is a real template element but
 * lives inside a `UFieldGroup`, which rounds/joins its children via Tailwind
 * `first:`/`last:`/`only:` selectors keyed off actual DOM sibling position -
 * wrapping it in a ref span would remove it from that sibling set and visibly break
 * the joined-button styling with "Unmatch"/the priority input. All four keep a
 * `class` hook instead and get synced into the same registry as the ref-based
 * targets below.
 */
const SYNCED_CLASS_SELECTORS: Partial<Record<TourKey, string>> = {
    'use-as-source': '.tour-use-as-source',
    'go-to-manga': '.tour-go-to-manga',
    'more-download-links': '.tour-more-download-links',
    'match-download-link': '.tour-match-download-link',
};

interface FirstDownloadStep extends TourStep {
    key?: TourKey;
    title: string;
    body: string;
}

interface TourApi {
    tour: UseTourReturn;
    setTourTarget: (key: string, el: HTMLElement | null) => void;
    maybeAutoStart: () => void;
    restart: () => Promise<void>;
}

function hasSeenTour(): boolean {
    if (!import.meta.client) return true;
    return localStorage.getItem(SEEN_KEY) === 'true';
}

function markTourSeen(): void {
    if (!import.meta.client) return;
    localStorage.setItem(SEEN_KEY, 'true');
}

/**
 * `useTour` must never be instantiated during SSR - this app is a real SSR Nuxt
 * server behind the YARP gateway, and module/plugin state created there would be
 * shared across every request handled by the same server process. This inert stub
 * keeps `useNuxtApp().$tour` safe to read from any component's top-level
 * `<script setup>` (not just from `onMounted`) regardless of whether that render
 * happens on the server or the client.
 */
function createInertTour(): TourApi {
    return {
        tour: {
            open: ref(false),
            index: ref(0),
            current: computed(() => undefined),
            reference: computed(() => undefined),
            total: computed(() => 0),
            hasNext: computed(() => false),
            hasPrev: computed(() => false),
            start: () => {},
            next: () => {},
            prev: () => {},
            goTo: () => {},
            finish: () => {},
        },
        setTourTarget: () => {},
        maybeAutoStart: () => {},
        restart: async () => {},
    };
}

export default defineNuxtPlugin({
    name: 'tour',
    setup() {
        if (!import.meta.client) {
            return { provide: { tour: createInertTour() } };
        }

        const targets = reactive<Record<TourKey, HTMLElement | null>>({
            'search-button': null,
            'use-as-source': null,
            'go-to-manga': null,
            'more-download-links': null,
            'add-link': null,
            'match-download-link': null,
        });

        const setTourTarget = (key: string, el: HTMLElement | null): void => {
            if ((TOUR_KEYS as readonly string[]).includes(key)) targets[key as TourKey] = el;
        };

        const steps: FirstDownloadStep[] = [
            {
                title: "Let's queue your first download",
                body: "We'll walk through finding a manga and downloading its first chapters. It only takes a minute.",
            },
            {
                key: 'search-button',
                target: () => targets['search-button'] ?? undefined,
                title: 'Search for a manga',
                body: 'Click here (or press ⌘K / Ctrl K) and type a title to search your configured extensions.',
            },
            {
                key: 'use-as-source',
                target: () => targets['use-as-source'] ?? undefined,
                title: 'Add it to your library',
                body: 'Picking a result creates a draft entry. Click this to make it the source for a new Manga.',
            },
            {
                key: 'go-to-manga',
                target: () => targets['go-to-manga'] ?? undefined,
                title: 'Open the Manga page',
                body: "This is where you'll manage download links and see progress.",
            },
            {
                key: 'more-download-links',
                target: () => targets['more-download-links'] ?? undefined,
                title: 'Find download sources',
                body: 'Search extensions for chapters of this series.',
            },
            {
                key: 'add-link',
                target: () => targets['add-link'] ?? undefined,
                title: 'Add a download link',
                body: 'Type a title (or paste a manga URL), choose an extension, then click Add Link.',
            },
            {
                key: 'match-download-link',
                target: () => targets['match-download-link'] ?? undefined,
                title: 'Confirm the match',
                body: "Click Match to confirm this is the right series — Tranga queues the download as soon as it's matched.",
            },
            {
                title: "You're all set!",
                body: 'Tranga will download chapters in the background. Check progress any time under Manga → Manga Downloads.',
            },
        ];

        const tour = useTour(steps);

        // Keeps the class-selector-only targets in sync with the DOM while the tour
        // is open. Scoped to "open" so it costs nothing the rest of the time, and it
        // only ever writes into the same registry the ref-based targets use - it
        // never decides when to advance a step.
        let observer: MutationObserver | undefined;
        const syncClassTargets = (): void => {
            for (const [key, selector] of Object.entries(SYNCED_CLASS_SELECTORS)) {
                const el = document.querySelector<HTMLElement>(selector);
                if (targets[key as TourKey] !== el) targets[key as TourKey] = el;
            }
        };
        watch(tour.open, (open) => {
            if (open) {
                syncClassTargets();
                observer = new MutationObserver(syncClassTargets);
                observer.observe(document.body, { childList: true, subtree: true });
            } else {
                observer?.disconnect();
                observer = undefined;
            }
        });

        // Advances the tour when the user clicks the element the *current* step
        // points at. No wait/timeout is needed even when the next step's target
        // doesn't exist yet (e.g. the user still has to search and pick a result) -
        // `tour.reference` is a computed that reads the registry reactively, so it
        // resolves on its own the moment that target gets registered.
        document.addEventListener(
            'click',
            (event) => {
                if (!tour.open.value) return;
                const key = (tour.current.value as FirstDownloadStep | undefined)?.key;
                if (!key) return;
                const el = targets[key];
                if (el && event.target instanceof Node && el.contains(event.target)) tour.next();
            },
            true,
        );

        const maybeAutoStart = (): void => {
            if (hasSeenTour()) return;
            markTourSeen();
            tour.start(0);
        };

        const restart = async (): Promise<void> => {
            if (useRoute().path !== '/') await navigateTo('/');
            await nextTick();
            markTourSeen();
            tour.start(0);
        };

        return { provide: { tour: { tour, setTourTarget, maybeAutoStart, restart } } };
    },
});
