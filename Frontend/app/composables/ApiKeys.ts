export namespace ApiKeys {
    export const DownloadExtensions = 'DownloadExtensions';
    export const MetadataExtensions = 'MetadataExtensions';
    export const SuwayomiStatus = 'SuwayomiStatus';
    export const SuwayomiExtensions = 'SuwayomiExtensions';

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

            export const List = (mangaId: string) => `Manga/${mangaId}/Chapters`;
        }
    }

    export namespace Tasks {
        export const Task = (id: string) => `Tasks/${id}`;

        export const Logs = (id: string) => `Tasks/${id}/Logs`;
    }

    export namespace Libraries {
        export const Mapping = (mangaId: string) => `Libraries/Mapping/${mangaId}`;

        export const Libraries = 'Libraries/Libraries';

        export const Library = (id: string) => `/Libraries/Libraries/${id}`;
    }

    export namespace Notifications {
        export const Extensions = 'Notifications/Extensions';

        export const Extension = (id: string) => `/Notifications/Extensions/${id}`;
    }

    export namespace Auth {
        export const Status = 'Auth/Status';

        export const ApiKeys = 'Auth/ApiKeys';
    }
}
