namespace FcaAssistant.App;

/// <summary>Lets a caller wake the poll loop so it runs its next cycle immediately instead of waiting for the interval.</summary>
public interface IRefreshTrigger
{
    void Trigger();
}
