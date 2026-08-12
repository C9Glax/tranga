namespace Common.Services;

/// <summary>
/// Base class for registering a service's endpoint tree under a route prefix. Implemented once per service
/// (e.g. Services.Manga's endpoints builder) and wired up via <see cref="Service.SetupWebApplication{TEndpointsBuilder}"/>.
/// </summary>
public abstract class EndpointsBuilder
{
    /// <summary>Maps a route group at <paramref name="prefix"/> on <paramref name="builder"/> and registers the service's endpoints into it.</summary>
    /// <param name="builder">The web application to register the route group on.</param>
    /// <param name="prefix">The route prefix all of the service's endpoints are nested under.</param>
    /// <returns>The route group, so callers can conditionally chain conventions (e.g. <c>.RequireAuthorization()</c>) onto it.</returns>
    public RouteGroupBuilder AddEndpoints(WebApplication builder, string prefix = "/")
    {
        RouteGroupBuilder routeGroupBuilder = builder.MapGroup(prefix);
        AddEndpoints(routeGroupBuilder);
        return routeGroupBuilder;
    }

    /// <summary>Registers the service's endpoints onto the given route group.</summary>
    /// <param name="builder">The route group to register endpoints on.</param>
    protected abstract void AddEndpoints(RouteGroupBuilder builder);
}