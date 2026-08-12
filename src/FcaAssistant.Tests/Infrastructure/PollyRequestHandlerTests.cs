using System.Net;
using FcaAssistant.Infrastructure;
using FcaAssistant.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace FcaAssistant.Tests.Infrastructure;

public class PollyRequestHandlerTests
{
    private static HttpMessageInvoker Invoker(StubHttpMessageHandler stub)
    {
        var handler = new PollyRequestHandler(NullLogger<PollyRequestHandler>.Instance) { InnerHandler = stub };
        return new HttpMessageInvoker(handler);
    }

    private static Task<HttpResponseMessage> SendAsync(HttpMessageInvoker invoker) =>
        invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.com"), CancellationToken.None);

    [Fact]
    public async Task SuccessfulResponse_PassesThroughWithoutRetry()
    {
        var stub = new StubHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.OK));

        var response = await SendAsync(Invoker(stub));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, stub.CallCount);
    }

    [Fact]
    public async Task FailingResponse_IsRetriedUntilSuccess()
    {
        var statuses = new Queue<HttpStatusCode>([HttpStatusCode.InternalServerError, HttpStatusCode.OK]);
        var stub = new StubHttpMessageHandler(() =>
            new HttpResponseMessage(statuses.Dequeue()) { Content = new StringContent("boom") });

        var response = await SendAsync(Invoker(stub));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, stub.CallCount);
    }
}
