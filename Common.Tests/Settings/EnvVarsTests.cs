using Common.Settings;

namespace Common.Tests.Settings;

// EnvVars fields are `static readonly`, read from the environment once at type-initialization
// time for the whole test process, so these tests can only assert the values baked in at that
// point (i.e. the defaults, assuming the test environment doesn't set these variables) rather
// than exercising the "overridden by environment variable" branch in-process.
public class EnvVarsTests
{
    [Fact]
    public void WorkersMinDefaultsToOne()
    {
        Assert.Equal(1, EnvVars.WorkersMin);
    }
}