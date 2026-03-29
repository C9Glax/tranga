import createClient from 'openapi-fetch';
import type { paths } from '~/composables/tranga-api';

export const api = createClient<paths>({ baseUrl: 'http://localhost:8080' });
