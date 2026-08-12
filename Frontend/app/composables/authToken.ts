const STORAGE_KEY = 'tranga-auth-token';

/**
 * The session JWT returned by `/auth/setup` and `/auth/login`, persisted in localStorage so it survives page
 * reloads. Read directly (rather than via a reactive ref) by the `tranga` $fetch instance in
 * `plugins/tranga.ts`, since that plugin attaches the header outside of Vue's reactivity.
 */
export function getAuthToken(): string | null {
    if (!import.meta.client) return null;
    return localStorage.getItem(STORAGE_KEY);
}

export function setAuthToken(token: string): void {
    if (!import.meta.client) return;
    localStorage.setItem(STORAGE_KEY, token);
}

export function clearAuthToken(): void {
    if (!import.meta.client) return;
    localStorage.removeItem(STORAGE_KEY);
}
