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

    // On the very first client navigation the server has already rendered `to` (it has no idea a
    // redirect is coming, since this check is client-only) and Vue hasn't mounted/hydrated yet. A
    // soft `navigateTo` here would hydrate the setup/login page's markup onto that mismatched
    // server-rendered DOM, leaving a broken layout until the next refresh. Forcing an external
    // navigation in that case makes the browser do a real reload so the target route gets its own
    // clean SSR render instead.
    const redirect = (path: string) => navigateTo(path, useNuxtApp().isHydrating ? { external: true } : undefined);

    if (!status.configured) return redirect('/setup');
    if (!getAuthToken()) return redirect('/login');
});
