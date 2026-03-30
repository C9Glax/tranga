export default {
    input: '../API/openapi/API.json',
    output: { path: `app/api/trangaApi`, postProcess: ['eslint', 'prettier'] },
    plugins: [
        { name: '@hey-api/typescript' },
        { name: 'zod', compatibilityVersion: 'mini', types: { infer: true }, requests: false, responses: false, definitions: false },
    ],
};
