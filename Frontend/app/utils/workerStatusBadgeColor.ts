import type { ServicesTasksWorkerStatus } from '~/api/tranga';

export const workerStatusBadgeColor = (status: ServicesTasksWorkerStatus) => {
    switch (status) {
        case 'Busy':
            return 'primary';
        case 'Retiring':
            return 'warning';
        case 'Idle':
        default:
            return 'neutral';
    }
};
