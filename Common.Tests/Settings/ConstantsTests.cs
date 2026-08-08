using System.Reflection;
using Common.Settings;

namespace Common.Tests.Settings;

public class ConstantsTests
{
    [Fact]
    public void OpenApiDocumentationRunIsFalseInNormalRuns()
    {
        // The xunit test host is not "GetDocument.Insider", so this should evaluate to false.
        // The true-case can't be exercised in-process since OpenApiDocumentationRun is computed
        // once from Assembly.GetEntryAssembly() at type-initialization time.
        Assert.False(Constants.OpenApiDocumentationRun);
    }

    [Fact]
    public void OpenApiDocumentationRunReflectsEntryAssemblyName()
    {
        bool expected = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
        Assert.Equal(expected, Constants.OpenApiDocumentationRun);
    }
}