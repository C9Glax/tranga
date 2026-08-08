using System.Net;
using Common.Helpers;

namespace Common.Tests.Helpers;

public class RequestClientTests(TestServerFixture serverFixture) : TrangaTest
{
    private sealed record TestDto(string Name, int Value);

    [Fact]
    public async Task SuccessfulRequest()
    {
        RequestClient client = new();
        HttpRequestMessage request = new(HttpMethod.Get, TestServerFixture.BaseUrl);

        HttpResponseMessage response = await client.SendAsync(request, ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void SetsUserAgentHeader()
    {
        RequestClient client = new();

        Assert.Single(client.DefaultRequestHeaders.UserAgent);
        Assert.Equal("Tranga/2.1", client.DefaultRequestHeaders.UserAgent.ToString());
    }

    [Fact]
    public async Task ConstructorWithRateLimiterUsesLimiterForRequests()
    {
        TestRateLimiter limiter = new();
        RequestClient client = new(limiter);
        HttpRequestMessage request = new(HttpMethod.Get, TestServerFixture.BaseUrl);

        HttpResponseMessage response = await client.SendAsync(request, ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, limiter.AcquireAsyncCallCount);
    }

    [Fact]
    public async Task ConstructorWithoutRateLimiterStillWorks()
    {
        RequestClient client = new();
        HttpRequestMessage request = new(HttpMethod.Get, TestServerFixture.BaseUrl);

        HttpResponseMessage response = await client.SendAsync(request, ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SendAsyncAndParseJsonParsesOnSuccess()
    {
        using MockHttpServer server = new(HttpStatusCode.OK, """{"name":"test","value":42}""");
        RequestClient client = new();
        HttpRequestMessage request = new(HttpMethod.Get, server.BaseUrl);

        TestDto? result = await client.SendAsyncAndParseJson<TestDto>(request, ct);

        Assert.NotNull(result);
        Assert.Equal("test", result.Name);
        Assert.Equal(42, result.Value);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task SendAsyncAndParseJsonReturnsNullOnError(HttpStatusCode statusCode)
    {
        using MockHttpServer server = new(statusCode, """{"name":"test","value":42}""");
        RequestClient client = new();
        HttpRequestMessage request = new(HttpMethod.Get, server.BaseUrl);

        TestDto? result = await client.SendAsyncAndParseJson<TestDto>(request, ct);

        Assert.Null(result);
    }
}