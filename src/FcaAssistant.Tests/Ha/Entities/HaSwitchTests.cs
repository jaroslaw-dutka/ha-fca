using FcaAssistant.Ha.Entities;

namespace FcaAssistant.Tests.Ha.Entities;

public class HaSwitchTests
{
    private static HaSwitch Create(Func<HaSwitch, string, Task>? action = null) =>
        new(TestDevice.Create(), "Climate", action ?? ((_, _) => Task.CompletedTask));

    [Fact]
    public void NewSwitch_IsOff()
    {
        var @switch = Create();

        Assert.False(@switch.IsOn);
        Assert.Equal("OFF", @switch.State);
    }

    [Fact]
    public void Metadata_IsDerivedFromDeviceAndName()
    {
        var @switch = Create();

        Assert.Equal("switch", @switch.Type);
        Assert.Equal("Climate", @switch.Name);
        Assert.Equal("VIN123_Climate", @switch.Id);
    }

    [Fact]
    public async Task OnSetAsync_On_TurnsOnAndReportsState()
    {
        var @switch = Create();

        await @switch.OnSetAsync("ON");

        Assert.True(@switch.IsOn);
        Assert.Equal("ON", @switch.State);
    }

    [Fact]
    public async Task OnSetAsync_Off_TurnsOff()
    {
        var @switch = Create();
        await @switch.OnSetAsync("ON");

        await @switch.OnSetAsync("OFF");

        Assert.False(@switch.IsOn);
        Assert.Equal("OFF", @switch.State);
    }

    [Fact]
    public async Task OnSetAsync_UnknownPayload_IsTreatedAsOff()
    {
        var @switch = Create();

        await @switch.OnSetAsync("whatever");

        Assert.False(@switch.IsOn);
    }

    [Fact]
    public async Task OnSetAsync_InvokesActionWithSwitchAndPayload()
    {
        HaSwitch? received = null;
        string? receivedState = null;
        var @switch = Create((s, state) =>
        {
            received = s;
            receivedState = state;
            return Task.CompletedTask;
        });

        await @switch.OnSetAsync("ON");

        Assert.Same(@switch, received);
        Assert.Equal("ON", receivedState);
    }

    [Fact]
    public void SetState_ChangesStateWithoutInvokingAction()
    {
        var invoked = false;
        var @switch = Create((_, _) => { invoked = true; return Task.CompletedTask; });

        @switch.SetState(true);
        Assert.True(@switch.IsOn);
        Assert.Equal("ON", @switch.State);

        @switch.SetState(false);
        Assert.False(@switch.IsOn);

        Assert.False(invoked);
    }

    [Fact]
    public void StateConstants_AreOnOff()
    {
        Assert.Equal("ON", HaSwitch.OnState);
        Assert.Equal("OFF", HaSwitch.OffState);
    }
}
