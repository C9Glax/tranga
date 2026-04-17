export default {
    input: ['../Services.Manga/openapi/Services.Manga.json', '../Services.Tasks/openapi/Services.Tasks.json'],
    output: { path: `app/api/tranga`, postProcess: ['eslint', 'prettier'] },
    plugins: [
        { name: '@hey-api/typescript' },
        { name: 'zod', compatibilityVersion: 'mini', types: { infer: true }, requests: false, responses: false, definitions: false },
    ],
};
