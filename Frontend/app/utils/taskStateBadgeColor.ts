import type { ServicesTasksTaskState } from '~/api/tranga';

export const taskStateBadgeColor = (state: ServicesTasksTaskState) => {
    switch (state) {
        case 'Running':
            return 'primary';
        case 'Queued':
            return 'info';
        case 'Blocked':
            return 'warning';
        case 'Completed':
            return 'secondary';
        case 'Failed':
            return 'error';
        case 'Pending':
        default:
            return 'neutral';
    }
};
