export type NsfwCoverMode = 'show' | 'click' | 'hide';

export const NSFW_COVER_MODE_KEY = 'tranga-nsfw-cover-mode';
export const NSFW_COVER_MODE_DEFAULT: NsfwCoverMode = 'click';

export function isNsfwCoverMode(value: unknown): value is NsfwCoverMode {
    return value === 'show' || value === 'click' || value === 'hide';
}

/**
 * How NSFW covers are displayed: always visible (`show`), blurred until clicked (`click`), or always blurred (`hide`).
 * Shared via `useState`, so SSR and the first client render use the default. The persisted value from localStorage
 * is applied (and later changes are written back) by `plugins/preferences.client.ts`, which avoids hydration mismatches.
 */
export const useNsfwCoverMode = () => useState<NsfwCoverMode>('nsfw-cover-mode', () => NSFW_COVER_MODE_DEFAULT);
