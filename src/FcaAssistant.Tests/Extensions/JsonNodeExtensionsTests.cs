using System.Text.Json.Nodes;
using FcaAssistant.Extensions;

namespace FcaAssistant.Tests.Extensions;

public class JsonNodeExtensionsTests
{
    [Fact]
    public void Flatten_ScalarValue_UsesKeyAsIs()
    {
        var node = JsonValue.Create("hello");

        var result = node!.Flatten("root");

        Assert.Equal(new Dictionary<string, string> { ["root"] = "hello" }, result);
    }

    [Fact]
    public void Flatten_DefaultKey_IsRoot()
    {
        var node = JsonValue.Create(42);

        var result = node!.Flatten();

        Assert.Equal("42", result["root"]);
    }

    [Fact]
    public void Flatten_Object_PrefixesEachKey()
    {
        var node = JsonNode.Parse("""{ "a": 1, "b": "x" }""");

        var result = node!.Flatten("car");

        Assert.Equal(new Dictionary<string, string>
        {
            ["car_a"] = "1",
            ["car_b"] = "x"
        }, result);
    }

    [Fact]
    public void Flatten_NestedObject_JoinsKeysWithUnderscore()
    {
        var node = JsonNode.Parse("""{ "battery": { "soc": 80 } }""");

        var result = node!.Flatten("car");

        Assert.Equal("80", result["car_battery_soc"]);
    }

    [Fact]
    public void Flatten_Array_IndexesElements()
    {
        var node = JsonNode.Parse("""{ "levels": [10, 20] }""");

        var result = node!.Flatten("car");

        Assert.Equal("10", result["car_levels_0"]);
        Assert.Equal("20", result["car_levels_1"]);
    }

    [Fact]
    public void Flatten_Booleans_AreStringified()
    {
        var node = JsonNode.Parse("""{ "charging": true }""");

        var result = node!.Flatten("car");

        Assert.Equal("true", result["car_charging"]);
    }
}
