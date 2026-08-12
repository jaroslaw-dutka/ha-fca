using FcaAssistant.App;

namespace FcaAssistant.Tests.Fakes;

public class FakeRefreshTrigger : IRefreshTrigger
{
    public int TriggerCount { get; private set; }

    public WaitHandle Requested { get; } = new AutoResetEvent(false);

    public void Trigger() => TriggerCount++;
}
