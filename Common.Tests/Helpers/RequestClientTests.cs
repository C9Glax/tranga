using System.Net;
using Common.Helpers;

namespace Common.Tests.Helpers;

public class RequestClientTests(TestServerFixture serverFixture) : TrangaTest
{
    
    [Fact]
    public async Task SuccessfulRequest()
    { 
        RequestClient client = new();
        HttpRequestMessage request = new(HttpMethod.Get, TestServerFixture.BaseUrl);

        HttpResponseMessage response = await client.SendAsync(request, ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}