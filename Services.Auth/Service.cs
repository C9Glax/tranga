using Common.Database.Auth;
using Common.Services.Authentication;
using Microsoft.EntityFrameworkCore;
using Services.Auth.Features;
using Constants = Common.Settings.Constants;

namespace Services.Auth;

/// <summary>
/// Entry point for the Auth service, which owns the single admin credential and the scoped API keys used by
/// the opt-in authorization system (see <see cref="Common.Settings.EnvVars.UseAuth"/>). Runs regardless of
/// whether <c>UseAuth</c> is enabled, so its schema/migrations are always in place and an admin can be set up
/// ahead of flipping the flag on.
/// </summary>
public sealed class Service : Common.Services.Service
{
    /// <param name="args">Command-line arguments passed to the service host.</param>
    public Service(string[] args) : base(args)
    {
        Builder.Services.AddAuthContextIfMissing();

        SetupWebApplication<Endpoints>("/auth");

        if (!Constants.OpenApiDocumentationRun)
        {
            using AuthContext context = App.Services.CreateScope().ServiceProvider.GetRequiredService<AuthContext>();
            context.Database.MigrateAsync(CancellationToken.None).Wait();
        }
    }

    /// <summary>The process entry point; constructs and runs the Auth service.</summary>
    /// <param name="args">Command-line arguments passed to the service host.</param>
    public static void Main(string[] args)
    {
        Service service = new(args);
        Task.WaitAll(service.Run());
    }
}
