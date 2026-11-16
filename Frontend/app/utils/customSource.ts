import { FetchError } from 'ofetch';
import type {
    PostMangasCustomDownloadersTestResponse,
    ServicesMangaPostCustomDownloaderTestRequest,
    ServicesMangaSelectorDownloaderDefinition,
    ServicesMangaSelectorField,
} from '~/api/tranga';

export type CustomSourceTestStep = 'search' | 'chapters' | 'images';

/** The latest test of a step: kept by the form (not by the step) so it stays visible next to every step. */
export type CustomSourceTestState = {
    step: CustomSourceTestStep;
    input: string;
    loading: boolean;
    result?: PostMangasCustomDownloadersTestResponse;
    /** Problems with the definition that keep the step from being tested (HTTP 400). */
    errors: string[];
};

/** What the website answers with: HTML pages (CSS selectors / XPath) or the JSON of an API (paths). */
export type CustomSourceFormat = 'html' | 'json';

/** One selector field as the form edits it: every part is a plain string (empty = not set). */
export type CustomSourceField = { selector: string; attribute: string; regex: string; replacement: string };

/** A custom source definition with every section present, as the form edits it. */
export type CustomSourceDefinition = {
    id: string;
    name: string;
    baseUrl: string;
    iconUrl: string;
    format: CustomSourceFormat;
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
    images: { urlTemplate: string; urlPattern: string; pageSelector: CustomSourceField; imageSelector: string; attribute: string };
};

export const emptyField = (): CustomSourceField => ({ selector: '', attribute: '', regex: '', replacement: '' });

export const emptyCustomSource = (): CustomSourceDefinition => ({
    id: '',
    name: '',
    baseUrl: '',
    iconUrl: '',
    format: 'html',
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
    images: { urlTemplate: '{url}', urlPattern: '', pageSelector: emptyField(), imageSelector: '', attribute: '' },
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
        // pasted JSON may spell it "JSON": the server only accepts the two lower case values, so it is corrected here
        format: String(source.format ?? '').toLowerCase() === 'json' ? 'json' : 'html',
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
            urlPattern: source.images?.urlPattern ?? '',
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

/**
 * What is sent to the API (and shown as JSON): empty optional values become null, blank header rows are dropped. JSON
 * has no attributes, so the ones left over from before the format was switched are not sent (they stay in the form, in
 * case the format is switched back).
 */
export const customSourceForApi = (source: CustomSourceDefinition): ServicesMangaSelectorDownloaderDefinition => {
    const json = source.format === 'json';
    const fieldForApi = (value: CustomSourceField): ServicesMangaSelectorField => ({
        selector: value.selector.trim(),
        attribute: json ? null : blankToNull(value.attribute),
        regex: blankToNull(value.regex),
        replacement: blankToNull(value.replacement),
    });
    return {
        id: source.id || undefined,
        name: source.name.trim(),
        baseUrl: normalizeBaseUrl(source.baseUrl),
        iconUrl: blankToNull(source.iconUrl),
        format: source.format,
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
            urlPattern: blankToNull(source.images.urlPattern),
            pageSelector: fieldForApi(source.images.pageSelector),
            imageSelector: source.images.imageSelector.trim(),
            attribute: json ? null : blankToNull(source.images.attribute),
        },
    };
};

/**
 * What is sent to try one step: the general settings and only the section of that step, so a step can be tried before the
 * other steps exist (the server validates exactly what it gets).
 */
export const customSourceTestBody = (
    source: CustomSourceDefinition,
    step: CustomSourceTestStep,
    input: string,
): ServicesMangaPostCustomDownloaderTestRequest => {
    const { search, chapters, images, ...settings } = customSourceForApi(source);
    return { settings, step, input, ...(step === 'search' ? { search } : step === 'chapters' ? { chapters } : { images }) };
};

/**
 * The messages of a failed API call. The API answers an invalid body with a validation problem
 * (`{ errors: { field: [messages] } }`); a list of messages or a plain text are understood as well.
 */
export const apiErrorMessages = (error: unknown, fallback: string): string[] => {
    if (error instanceof FetchError) {
        const data: unknown = error.data;
        if (Array.isArray(data)) return data.map(String);
        if (typeof data === 'string' && data) return [data];
        if (data && typeof data === 'object') {
            const { errors, title } = data as { errors?: unknown; title?: unknown };
            if (errors && typeof errors === 'object') {
                const messages = Object.values(errors).flatMap((value) => (Array.isArray(value) ? value.map(String) : [String(value)]));
                if (messages.length) return [...new Set(messages)];
            }
            if (typeof title === 'string' && title) return [title];
        }
    }
    return [fallback];
};

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
