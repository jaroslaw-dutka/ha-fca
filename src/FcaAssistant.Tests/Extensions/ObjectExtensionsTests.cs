using FcaAssistant.Extensions;

namespace FcaAssistant.Tests.Extensions;

public class ObjectExtensionsTests
{
    [Fact]
    public void Dump_String_IsReturnedAsIs()
    {
        Assert.Equal("plain text", "plain text".Dump());
    }

    [Fact]
    public void Dump_Null_ReturnsJsonNull()
    {
        object? value = null;

        Assert.Equal("null", value.Dump());
    }

    [Fact]
    public void Dump_Object_IsSerializedToJson()
    {
        var result = new { Name = "Panda", Doors = 5 }.Dump();

        Assert.Contains("\"Name\": \"Panda\"", result);
        Assert.Contains("\"Doors\": 5", result);
    }
}
