using FcaAssistant.App;

namespace FcaAssistant.Tests.Fakes;

public class FakeRefreshTrigger : IRefreshTrigger
{
    public int TriggerCount { get; private set; }

    public void Trigger() => TriggerCount++;
}
