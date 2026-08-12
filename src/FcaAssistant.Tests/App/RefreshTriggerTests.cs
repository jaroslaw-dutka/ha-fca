using FcaAssistant.App;

namespace FcaAssistant.Tests.App;

public class RefreshTriggerTests
{
    private readonly RefreshTrigger _trigger = new();

    [Fact]
    public void NewTrigger_IsNotSignaled()
    {
        Assert.False(_trigger.Requested.WaitOne(0));
    }

    [Fact]
    public void Trigger_SignalsRequested()
    {
        _trigger.Trigger();

        Assert.True(_trigger.Requested.WaitOne(0));
    }

    [Fact]
    public void Requested_AutoResetsAfterBeingObserved()
    {
        _trigger.Trigger();

        Assert.True(_trigger.Requested.WaitOne(0));  // consumes the signal
        Assert.False(_trigger.Requested.WaitOne(0)); // auto-reset, no lingering signal
    }
}
