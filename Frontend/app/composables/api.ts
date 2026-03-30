import createClient from 'openapi-fetch';
import type { paths } from '~/composables/tranga-api';
import appConfig from '~/app.config';

export const api = createClient<paths>({ baseUrl: appConfig.api.baseUrl });
