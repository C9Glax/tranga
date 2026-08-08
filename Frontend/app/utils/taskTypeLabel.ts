const labels: Record<string, string> = {
    DbFileCleanupTask: 'File cleanup',
    GetMangaChaptersTask: 'Get chapters',
    MissingChapterScanTask: 'Missing chapter scan',
    PeriodicMangaChapterFetcherTask: 'Periodic chapter fetch',
};

export const taskTypeLabel = (taskTypeName: string): string => labels[taskTypeName] ?? splitCamelCase(taskTypeName);
