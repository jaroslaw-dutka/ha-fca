using System.Collections.Specialized;
using FcaAssistant.Extensions;

namespace FcaAssistant.Tests.Extensions;

public class NameValueCollectionExtensionsTests
{
    [Fact]
    public void ToDictionary_CopiesKeysAndValues()
    {
        var nvc = new NameValueCollection { { "a", "1" }, { "b", "2" } };

        var result = nvc.ToDictionary();

        Assert.Equal(new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" }, result);
    }

    [Fact]
    public void ToDictionary_NullValue_BecomesEmptyString()
    {
        var nvc = new NameValueCollection { { "a", null } };

        var result = nvc.ToDictionary();

        Assert.Equal("", result["a"]);
    }

    [Fact]
    public void ToDictionary_Empty_ReturnsEmpty()
    {
        var result = new NameValueCollection().ToDictionary();

        Assert.Empty(result);
    }
}
