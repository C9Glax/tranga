export default {
    input: '../Services.Manga/openapi/Services.Manga.json',
    output: { path: `app/api/trangaApi`, postProcess: ['eslint', 'prettier'] },
    plugins: [
        { name: '@hey-api/typescript' },
        { name: 'zod', compatibilityVersion: 'mini', types: { infer: true }, requests: false, responses: false, definitions: false },
    ],
};
