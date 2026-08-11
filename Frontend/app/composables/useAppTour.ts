import type { TourStep, UseTourReturn } from '@nuxt/ui/composables/useTour';

const TOUR_SEEN_KEY = 'tranga:tour:seen';

export const TOUR_STEPS: TourStep[] = [
    { title: 'Welcome to Tranga!', body: "Let's take a quick look at how to download your first manga." },
    {
        target: '#tour-search-button',
        title: 'Search for a manga',
        body: 'Click here (or press ctrl+k) and type a title to search local and remote sources.',
    },
    {
        target: '#tour-metadata-action',
        title: 'Pick your source',
        body: 'Review the metadata, then use it as the source for the manga. Once it\'s set, click "Go to Manga" to continue.',
    },
    { target: '.tour-match-target', title: 'Match a download-link', body: 'Found a link you like? Click "Match" to start downloading it.' },
];

function hasSeenTour(): boolean {
    if (!import.meta.client) return true;
    return localStorage.getItem(TOUR_SEEN_KEY) === 'true';
}

function markTourSeen(): void {
    if (!import.meta.client) return;
    localStorage.setItem(TOUR_SEEN_KEY, 'true');
}

let instance: UseTourReturn | undefined;

/**
 * `useTour` must never be instantiated during SSR: the resulting refs would be
 * shared module state across every request handled by the same server process.
 * `TourOverlay` (the only place the reactive `tour` state is actually bound to
 * the DOM) is mounted behind `<ClientOnly>`, so this inert stub is only ever
 * touched by SSR passes of other callers (e.g. the Settings page grabbing
 * `replay`) and is never rendered.
 */
function createInertTour(): UseTourReturn {
    return {
        open: ref(false),
        index: ref(0),
        current: computed(() => undefined),
        reference: computed(() => undefined),
        total: computed(() => TOUR_STEPS.length),
        hasNext: computed(() => false),
        hasPrev: computed(() => false),
        start: () => {},
        next: () => {},
        prev: () => {},
        goTo: () => {},
        finish: () => {},
    };
}

function getTour(): UseTourReturn {
    if (!import.meta.client) return createInertTour();
    instance ??= useTour(TOUR_STEPS);
    return instance;
}

export default function useAppTour() {
    const tour = getTour();

    const finishAndPersist = (): void => {
        tour.finish();
        markTourSeen();
    };

    const startIfFirstVisit = (): void => {
        if (!hasSeenTour()) tour.start();
    };

    const replay = (): void => {
        tour.start(0);
    };

    return { tour, hasSeenTour, finishAndPersist, startIfFirstVisit, replay };
}
