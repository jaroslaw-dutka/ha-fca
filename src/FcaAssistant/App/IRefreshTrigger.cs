namespace FcaAssistant.App;

/// <summary>
/// The "run the poll loop now" signal. Command handlers call <see cref="Trigger"/>; the poll loop
/// waits on <see cref="Requested"/> to run its next cycle immediately instead of waiting for the interval.
/// </summary>
public interface IRefreshTrigger
{
    WaitHandle Requested { get; }

    void Trigger();
}
