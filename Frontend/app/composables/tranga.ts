import type { UseFetchOptions } from '#app';

export function useTranga<T>(url: string | (() => string), options: UseFetchOptions<T> = {}) {
    return useFetch(url, { ...options, server: false, $fetch: useNuxtApp().$tranga });
}
