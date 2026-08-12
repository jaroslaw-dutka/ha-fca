namespace FcaAssistant.Tests.Fakes;

/// <summary>Test inner handler that returns a response from <paramref name="responder"/> and counts calls.</summary>
public class StubHttpMessageHandler(Func<HttpResponseMessage> responder) : HttpMessageHandler
{
    public int CallCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(responder());
    }
}
