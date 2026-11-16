import type { ServicesTasksTaskState } from '~/api/tranga';

export const taskStateLabel = (state: ServicesTasksTaskState): string => useNuxtApp().$i18n.t(`tasks.state.${state}.label`);
