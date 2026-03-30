import appConfig from '~/app.config';

export const useTranga = createUseFetch({ baseURL: appConfig.api.baseUrl });
