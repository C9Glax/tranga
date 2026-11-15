import type { ServicesMangaSelectorDownloaderDefinition, ServicesMangaSelectorField } from '~/api/tranga';

/** One selector field as the form edits it: every part is a plain string (empty = not set). */
export type CustomSourceField = { selector: string; attribute: string; regex: string; replacement: string };

/** A custom source definition with every section present, as the form edits it. */
export type CustomSourceDefinition = {
    id: string;
    name: string;
    baseUrl: string;
    iconUrl: string;
    languages: string[];
    useFlareSolverr: boolean;
    requestsPerSecond: number;
    headers: Record<string, string>;
    seriesUrlPattern: string;
    search: { urlTemplate: string; itemSelector: string; title: CustomSourceField; url: CustomSourceField; cover: CustomSourceField };
    chapters: {
        urlTemplate: string;
        itemSelector: string;
        url: CustomSourceField;
        number: CustomSourceField;
        title: CustomSourceField;
        date: CustomSourceField;
        dateFormat: string;
        nextPage: CustomSourceField;
    };
    images: { urlTemplate: string; pageSelector: CustomSourceField; imageSelector: string; attribute: string };
};

export const emptyField = (): CustomSourceField => ({ selector: '', attribute: '', regex: '', replacement: '' });

export const emptyCustomSource = (): CustomSourceDefinition => ({
    id: '',
    name: '',
    baseUrl: '',
    iconUrl: '',
    languages: ['en'],
    useFlareSolverr: false,
    requestsPerSecond: 1,
    headers: {},
    seriesUrlPattern: '',
    search: { urlTemplate: '', itemSelector: '', title: emptyField(), url: emptyField(), cover: emptyField() },
    chapters: {
        urlTemplate: '{url}',
        itemSelector: '',
        url: emptyField(),
        number: emptyField(),
        title: emptyField(),
        date: emptyField(),
        dateFormat: '',
        nextPage: emptyField(),
    },
    images: { urlTemplate: '{url}', pageSelector: emptyField(), imageSelector: '', attribute: '' },
});

const field = (source: ServicesMangaSelectorField | undefined): CustomSourceField => ({
    selector: source?.selector ?? '',
    attribute: source?.attribute ?? '',
    regex: source?.regex ?? '',
    replacement: source?.replacement ?? '',
});

/** Fills in everything a (possibly partial) definition from the API or pasted JSON leaves out. */
export const normalizeCustomSource = (source: ServicesMangaSelectorDownloaderDefinition | undefined): CustomSourceDefinition => {
    const defaults = emptyCustomSource();
    if (!source) return defaults;
    return {
        id: source.id ?? '',
        name: source.name ?? '',
        baseUrl: source.baseUrl ?? '',
        iconUrl: source.iconUrl ?? '',
        languages: source.languages?.length ? [...source.languages] : defaults.languages,
        useFlareSolverr: source.useFlareSolverr ?? false,
        requestsPerSecond: Number(source.requestsPerSecond ?? 1) || 1,
        headers: { ...(source.headers ?? {}) },
        seriesUrlPattern: source.seriesUrlPattern ?? '',
        search: {
            urlTemplate: source.search?.urlTemplate ?? '',
            itemSelector: source.search?.itemSelector ?? '',
            title: field(source.search?.title),
            url: field(source.search?.url),
            cover: field(source.search?.cover),
        },
        chapters: {
            urlTemplate: source.chapters?.urlTemplate ?? defaults.chapters.urlTemplate,
            itemSelector: source.chapters?.itemSelector ?? '',
            url: field(source.chapters?.url),
            number: field(source.chapters?.number),
            title: field(source.chapters?.title),
            date: field(source.chapters?.date),
            dateFormat: source.chapters?.dateFormat ?? '',
            nextPage: field(source.chapters?.nextPage),
        },
        images: {
            urlTemplate: source.images?.urlTemplate ?? defaults.images.urlTemplate,
            pageSelector: field(source.images?.pageSelector),
            imageSelector: source.images?.imageSelector ?? '',
            attribute: source.images?.attribute ?? '',
        },
    };
};

const blankToNull = (value: string) => (value.trim() === '' ? null : value.trim());

/** A language code as the server accepts it: `en`, `en-us`, `pt-BR`, `zh-Hans`, `es-419` (not just any word). */
export const isLanguageCode = (value: string): boolean => /^[a-z]{2,3}(-[a-z]{4})?(-([a-z]{2}|\d{3}))?$/i.test(value.trim());

/** Trims the base URL and removes trailing slashes: `https://example.com/` is the same website as `https://example.com`. */
export const normalizeBaseUrl = (value: string): string => value.trim().replace(/\/+$/, '');

export type SeriesPatternResult =
    | { state: 'empty' }
    | { state: 'invalid'; message: string }
    | { state: 'no-match' }
    | { state: 'match'; groups: { name: string; value: string }[] };

/**
 * Tries a series URL pattern on a URL, for the preview next to the pattern. Runs in the browser (JavaScript regex): the
 * server uses .NET, which understands the same everyday syntax (named groups, classes, lookaheads) but not every extra.
 */
export const testSeriesPattern = (pattern: string, url: string): SeriesPatternResult => {
    if (!pattern.trim() || !url.trim()) return { state: 'empty' };
    let regex: RegExp;
    try {
        regex = new RegExp(pattern.trim());
    } catch (error: unknown) {
        return { state: 'invalid', message: error instanceof Error ? error.message : 'Not a valid regular expression.' };
    }
    const match = regex.exec(url.trim());
    if (!match) return { state: 'no-match' };
    return { state: 'match', groups: Object.entries(match.groups ?? {}).map(([name, value]) => ({ name, value: value ?? '' })) };
};

const fieldForApi = (source: CustomSourceField): ServicesMangaSelectorField => ({
    selector: source.selector.trim(),
    attribute: blankToNull(source.attribute),
    regex: blankToNull(source.regex),
    replacement: blankToNull(source.replacement),
});

/** What is sent to the API (and shown as JSON): empty optional values become null, blank header rows are dropped. */
export const customSourceForApi = (source: CustomSourceDefinition): ServicesMangaSelectorDownloaderDefinition => ({
    id: source.id || undefined,
    name: source.name.trim(),
    baseUrl: normalizeBaseUrl(source.baseUrl),
    iconUrl: blankToNull(source.iconUrl),
    languages: source.languages.map((language) => language.trim()).filter(Boolean),
    useFlareSolverr: source.useFlareSolverr,
    requestsPerSecond: source.requestsPerSecond,
    headers: Object.fromEntries(Object.entries(source.headers).filter(([name]) => name.trim() !== '')),
    seriesUrlPattern: blankToNull(source.seriesUrlPattern),
    search: {
        urlTemplate: source.search.urlTemplate.trim(),
        itemSelector: source.search.itemSelector.trim(),
        title: fieldForApi(source.search.title),
        url: fieldForApi(source.search.url),
        cover: fieldForApi(source.search.cover),
    },
    chapters: {
        urlTemplate: source.chapters.urlTemplate.trim(),
        itemSelector: source.chapters.itemSelector.trim(),
        url: fieldForApi(source.chapters.url),
        number: fieldForApi(source.chapters.number),
        title: fieldForApi(source.chapters.title),
        date: fieldForApi(source.chapters.date),
        dateFormat: blankToNull(source.chapters.dateFormat),
        nextPage: fieldForApi(source.chapters.nextPage),
    },
    images: {
        urlTemplate: source.images.urlTemplate.trim(),
        pageSelector: fieldForApi(source.images.pageSelector),
        imageSelector: source.images.imageSelector.trim(),
        attribute: blankToNull(source.images.attribute),
    },
});

/** The JSON that is exported: like the API payload, but without the id (it is assigned on import). */
export const customSourceToJson = (source: CustomSourceDefinition): string => {
    const { id: _id, ...withoutId } = customSourceForApi(source);
    return JSON.stringify(withoutId, null, 2);
};

/** Parses pasted JSON into a definition. Returns an error message instead when it is not usable. */
export const customSourceFromJson = (json: string): { source?: CustomSourceDefinition; error?: string } => {
    try {
        const parsed: unknown = JSON.parse(json);
        if (typeof parsed !== 'object' || parsed === null || Array.isArray(parsed)) return { error: 'The JSON has to be an object.' };
        return { source: normalizeCustomSource(parsed as ServicesMangaSelectorDownloaderDefinition) };
    } catch (error: unknown) {
        return { error: error instanceof Error ? error.message : 'That is not valid JSON.' };
    }
};
