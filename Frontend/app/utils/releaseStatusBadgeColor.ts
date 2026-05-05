import type { ServicesMangaReleaseStatus } from '~/api/tranga';

export const releaseStatusBadgeColor = (status: ServicesMangaReleaseStatus) => {
    switch (status) {
        case 'Ongoing':
            return 'primary';
        case 'Hiatus':
            return 'warning';
        case 'Complete':
            return 'secondary';
        case 'Cancelled':
            return 'error';
        default:
            return 'neutral';
    }
};
