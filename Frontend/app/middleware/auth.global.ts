import type { ServicesAuthAuthStatusResponse } from '~/api/tranga';
import { getAuthToken } from '~/composables/authToken';

/**
 * Gates every page behind the setup/login flow when the backend has `UseAuth` enabled. Runs client-only -
 * `useTranga` throughout this app is already `server: false`, and the token itself only lives in localStorage,
 * so there is nothing meaningful to check during SSR.
 */
export default defineNuxtRouteMiddleware(async (to) => {
    if (import.meta.server) return;
    if (to.path === '/setup' || to.path === '/login') return;

    let status: ServicesAuthAuthStatusResponse;
    try {
        status = await useNuxtApp().$tranga<ServicesAuthAuthStatusResponse>('/auth/status');
    } catch {
        return;
    }

    if (!status.enabled) return;
    if (!status.configured) return navigateTo('/setup');
    if (!getAuthToken()) return navigateTo('/login');
});
