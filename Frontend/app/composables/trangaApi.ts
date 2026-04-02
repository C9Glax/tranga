import appConfig from '~/app.config';
import type { NitroFetchOptions, NitroFetchRequest } from 'nitropack';

export const useTranga = createUseFetch({ baseURL: appConfig.api.baseUrl, server: false });

export const $tranga = <T>(
    a: string,
    b:
        | NitroFetchOptions<NitroFetchRequest, 'get' | 'head' | 'patch' | 'post' | 'put' | 'delete' | 'connect' | 'options' | 'trace'>
        | undefined
) => $fetch<T>(appConfig.api.baseUrl + a, b);
