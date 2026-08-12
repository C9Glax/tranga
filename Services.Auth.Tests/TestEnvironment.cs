using System.Runtime.CompilerServices;

namespace Services.Auth.Tests;

/// <summary>
/// Sets <c>AUTH_SIGNING_KEY</c> before any test runs, so <see cref="Common.Settings.EnvVars.AuthSigningKey"/> -
/// a <c>static readonly</c> field read once on first access - always sees a value in this test assembly,
/// regardless of which test happens to touch it first.
/// </summary>
internal static class TestEnvironment
{
    [ModuleInitializer]
    internal static void SetAuthSigningKey() =>
        Environment.SetEnvironmentVariable("AUTH_SIGNING_KEY", "test-signing-key-not-for-production");
}
