using FcaAssistant.Extensions;
using Microsoft.Extensions.Logging.Abstractions;

namespace FcaAssistant.Tests.Extensions;

public class FlurlExtensionsTests
{
    [Fact]
    public async Task DumpResponseAsync_ReturnsAwaitedResult()
    {
        var result = await Task.FromResult(new { Value = 42 }).DumpResponseAsync(NullLogger.Instance);

        Assert.Equal(42, result.Value);
    }
}
