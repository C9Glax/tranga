export namespace ApiKeys {
    export const DownloadExtensions = 'DownloadExtensions';
    export const MetadataExtensions = 'MetadataExtensions';

    export namespace Manga {
        export const Manga = (id: string) => `Manga/${id}`;

        export const List = 'Manga/List';

        export const DownloadLinks = (id: string) => `Manga/${id}/DownloadLinks`;

        export const RelatedMetadata = (id: string) => `Manga/${id}/Metadata/Related`;

        export namespace Metadata {
            export const Entry = (id: string) => `Manga/Metadata/${id}`;

            export const Manga = (id: string) => `Manga/Metadata/${id}/Manga`;

            export const List = 'MetadataList';

            export const RelatedManga = (id: string) => `Manga/Metadata/${id}/RelatedManga`;
        }

        export namespace Chapters {
            export const Chapter = (id: string) => `Chapter/${id}`;
        }
    }

    export namespace Tasks {}

    export namespace Notifications {
        export const Extensions = 'Notifications/Extensions';

        export const Extension = (id: string) => `/Notifications/Extensions/${id}`;
    }
}
