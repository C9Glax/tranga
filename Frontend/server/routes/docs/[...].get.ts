// Proxies sub-paths under /docs ("/docs/scalar.js", "/docs/openapi/...") to the scalar-docs container. The root
// itself ("/docs", "/docs/") is handled by the sibling server/routes/docs.get.ts instead: this catch-all doesn't
// match its own parent path when there are zero remaining segments, even with a trailing slash.
export default defineEventHandler(async (event) => {
    const targetPath = event.path.replace(/^\/docs/, '');
    return proxyRequest(event, `${useRuntimeConfig(event).scalarDocsUrl}${targetPath}`);
});
