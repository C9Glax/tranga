namespace Extensions.Tests;

public abstract class ExtensionsTests<T>(IExtension<T> extension) where T : IExtension<T>
{
    [Fact]
    public void Test1()
    {
    }
}