import type { ServicesTasksTaskState } from '~/api/tranga';

export const taskStateDescription = (state: ServicesTasksTaskState): string => {
    switch (state) {
        case 'Pending':
            return 'Not yet queued; either not due, or blocked on a dependency.';
        case 'Blocked':
            return 'Due to run, but blocked on an unfinished dependency.';
        case 'Queued':
            return 'In the ready queue, waiting for a worker to pick it up.';
        case 'Running':
            return 'Currently executing on a worker.';
        case 'Completed':
            return 'Finished successfully.';
        case 'Failed':
            return 'Finished with an exception.';
    }
};
