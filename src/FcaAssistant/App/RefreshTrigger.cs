namespace FcaAssistant.App;

/// <summary>
/// Owns the "run the poll loop now" signal. Command handlers call <see cref="Trigger"/>; the poll loop
/// waits on <see cref="Requested"/>. Kept separate from <see cref="AppLoop"/> so it stays a
/// dependency leaf — otherwise AppLoop -> handlers -> IRefreshTrigger -> AppLoop would be a DI cycle.
/// </summary>
public class RefreshTrigger : IRefreshTrigger
{
    private readonly AutoResetEvent _requested = new(false);

    public WaitHandle Requested => _requested;

    public void Trigger() => _requested.Set();
}
