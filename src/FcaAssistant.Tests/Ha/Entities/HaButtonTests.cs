using FcaAssistant.Ha.Entities;

namespace FcaAssistant.Tests.Ha.Entities;

public class HaButtonTests
{
    [Fact]
    public void Metadata_IsDerivedFromDeviceAndName()
    {
        var button = new HaButton(TestDevice.Create(), "Blink", (_, _) => Task.CompletedTask);

        Assert.Equal("button", button.Type);
        Assert.Equal("Blink", button.Name);
        Assert.Equal("VIN123_Blink", button.Id);
    }

    [Fact]
    public async Task OnSetAsync_InvokesActionWithButtonAndPayload()
    {
        HaButton? received = null;
        string? receivedState = null;
        var button = new HaButton(TestDevice.Create(), "Blink", (b, state) =>
        {
            received = b;
            receivedState = state;
            return Task.CompletedTask;
        });

        await button.OnSetAsync("PRESS");

        Assert.Same(button, received);
        Assert.Equal("PRESS", receivedState);
    }
}
