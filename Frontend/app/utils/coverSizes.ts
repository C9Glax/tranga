export interface CoverSize {
    width: number;
    height: number;
}

export const COVER_SIZES = {
    tiny: { width: 24, height: 36 }, // Manga/Search.vue command-palette icon
    card: { width: 240, height: 360 }, // Manga/List.vue grid card, Manga/MergeConfirm.vue preview
    hero: { width: 256, height: 384 }, // Manga/Page.vue and Metadata/Page.vue hero
    blogCard: { width: 360, height: 249 }, // Metadata/ListCard.vue, DownloadLink/ListCard.vue (13:9 UBlogPost header)
} as const satisfies Record<string, CoverSize>;

export function withCoverSize(path: string, size: CoverSize): string {
    const separator = path.includes('?') ? '&' : '?';
    return `${path}${separator}width=${size.width}&height=${size.height}`;
}
