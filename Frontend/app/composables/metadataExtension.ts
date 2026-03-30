const icons: Map<string, { icon: string; name: string }> = new Map([
    ['019cf2cb-3aac-7c9c-9580-7091471b6788', { icon: 'https://www.mangaupdates.com/images/manga-updates.svg', name: 'Manga Updates' }],
]);

export class MetadataExtensions {
    static GetIcon = (extensionId: string) => icons.get(extensionId)?.icon ?? '';
    static GetName = (extensionId: string) => icons.get(extensionId)?.name ?? '';
}
