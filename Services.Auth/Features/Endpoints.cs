using Common.Services;
using Services.Auth.Features.ApiKeys;
using Services.Auth.Features.Auth;

namespace Services.Auth.Features;

/// <summary>
/// Root endpoint builder for the Auth service. <c>/status</c>, <c>/setup</c> and <c>/login</c> are always
/// anonymous - they're the entry point into the flow, so they must stay reachable even when
/// <see cref="Common.Settings.EnvVars.UseAuth"/> makes every other route group require a credential. The
/// <c>/apikeys</c> group has no such override, so it inherits that requirement normally.
/// </summary>
public sealed class Endpoints : EndpointsBuilder
{
    /// <inheritdoc />
    protected override void AddEndpoints(RouteGroupBuilder builder)
    {
        builder.MapGroup(string.Empty)
            .WithTags("Auth")
            .AllowAnonymous()
            .MapAuthEndpoints();

        builder.MapGroup("/apikeys")
            .WithTags("Auth", "ApiKeys")
            .MapApiKeyEndpoints();
    }
}

/// <summary>Extension methods that register the Auth service's endpoints on a route group.</summary>
internal static class EndpointHelpers
{
    /// <summary>Maps <c>/status</c>, <c>/setup</c>, and <c>/login</c>.</summary>
    internal static void MapAuthEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet("/status", GetStatusEndpoint.Handle)
            .WithSummary("Get the current authorization status.");

        builder.MapPost("/setup", PostSetupEndpoint.Handle)
            .WithSummary("Create the admin password (first run only).");

        builder.MapPost("/login", PostLoginEndpoint.Handle)
            .WithSummary("Log in with the admin password.");
    }

    /// <summary>Maps the CRUD endpoints for API keys: creation, listing, and revocation.</summary>
    internal static void MapApiKeyEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapPost(string.Empty, PostApiKeyEndpoint.Handle)
            .WithSummary("Create a new API key.");

        builder.MapGet(string.Empty, GetApiKeysEndpoint.Handle)
            .WithSummary("List API keys (metadata only).");

        builder.MapDelete("{apiKeyId}", DeleteApiKeyEndpoint.Handle)
            .WithSummary("Revoke an API key.");
    }
}
