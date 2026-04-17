namespace Common.Services;

public abstract class EndpointsBuilder
{
    public void AddEndpoints(WebApplication builder, string prefix = "/")
    {
        RouteGroupBuilder routeGroupBuilder = builder.MapGroup(prefix);
        AddEndpoints(routeGroupBuilder);
    }

    protected abstract void AddEndpoints(RouteGroupBuilder builder);
}