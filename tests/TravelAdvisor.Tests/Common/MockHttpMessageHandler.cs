using System.Net;
using System.Text;

namespace TravelAdvisor.Tests.Common;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly string _response;

    public MockHttpMessageHandler(string response)
    {
        _response = response;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(_response, Encoding.UTF8, "application/json")
        });
    }
}

public class FailingHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        throw new HttpRequestException("Simulated network failure");
    }
}
