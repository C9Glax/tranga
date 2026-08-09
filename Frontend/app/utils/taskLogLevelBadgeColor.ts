export const taskLogLevelBadgeColor = (level: string) => {
    switch (level) {
        case 'Critical':
            return 'error';
        case 'Error':
            return 'error';
        case 'Warning':
            return 'warning';
        case 'Information':
            return 'primary';
        case 'Debug':
        case 'Trace':
        default:
            return 'neutral';
    }
};
