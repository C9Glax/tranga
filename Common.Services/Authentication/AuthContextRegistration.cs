using Common.Database.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Services.Authentication;

/// <summary>
/// Registers <see cref="AuthContext"/> at most once. Both <see cref="JwtAuthenticationExtensions.AddTrangaAuthentication"/>
/// (needed by every other service, but only when <see cref="Common.Settings.EnvVars.UseAuth"/> is on, purely to
/// validate the <c>X-Api-Key</c> header) and <c>Services.Auth</c> itself (needed unconditionally, for
/// migrations/setup/login regardless of the flag) call this, so whichever runs first wins without double-registering.
/// </summary>
public static class AuthContextRegistration
{
    /// <summary>Registers <see cref="AuthContext"/> if it isn't already registered.</summary>
    public static IServiceCollection AddAuthContextIfMissing(this IServiceCollection services)
    {
        if (services.All(sd => sd.ServiceType != typeof(AuthContext)))
            services.AddDbContext<AuthContext>();

        return services;
    }
}
