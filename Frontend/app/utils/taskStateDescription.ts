import type { ServicesTasksTaskState } from '~/api/tranga';

export const taskStateDescription = (state: ServicesTasksTaskState): string => useNuxtApp().$i18n.t(`tasks.state.${state}.description`);
