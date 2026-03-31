const icons: Map<string, { icon: string; name: string }> = new Map([
    ['019ce521-deaf-7739-9e14-eb6f4afc86e2', { icon: 'https://mangadex.org/img/brand/mangadex-logo.svg', name: 'MangaDex' }],
]);

export class DownloadExtensions {
    static GetIcon = (extensionId?: string) => icons.get(extensionId ?? '')?.icon ?? '';
    static GetName = (extensionId?: string) => icons.get(extensionId ?? '')?.name ?? '';
}
