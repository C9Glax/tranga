// Handles exactly "/docs" and "/docs/" - the root of the combined API docs page (scalar-docs container, see
// Tranga.AppHost/AppHost.cs). Sub-paths ("/docs/scalar.js", "/docs/openapi/...") go through
// server/routes/docs/[...].get.ts instead: Nitro's catch-all route doesn't match its own parent path with zero
// remaining segments, so this file covers exactly the case that one can't.
//
// The container's own HTML references its script with a bare relative path ("scalar.js", not "/docs/scalar.js"),
// which only resolves correctly when the browser's URL ends in "/" - reached without it, the page renders blank.
// ASP.NET Core/YARP's path matching treats "/docs" and "/docs/" as equivalent, so this can't be fixed with a
// gateway route matching only the bare path; the raw path is available here as a plain string instead.
export default defineEventHandler(async (event) => {
    if (event.path === '/docs') {
        return sendRedirect(event, '/docs/', 301);
    }

    return proxyRequest(event, `${useRuntimeConfig(event).scalarDocsUrl}/`);
});
