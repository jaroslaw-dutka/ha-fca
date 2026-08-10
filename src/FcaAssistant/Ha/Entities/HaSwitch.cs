using FcaAssistant.Ha.Model;

namespace FcaAssistant.Ha.Entities;

public class HaSwitch(HaDevice device, string name, Func<HaSwitch, string, Task> setAction)
    : HaEntity(device, "switch", name), IHaStateEntity
{
    public const string OnState = "ON";
    public const string OffState = "OFF";

    public bool IsOn { get; private set; }
    public string State => IsOn ? OnState : OffState;

    public async Task OnSetAsync(string state)
    {
        IsOn = state == OnState;
        await setAction(this, state);
    }

    public void SetState(bool isOn) => IsOn = isOn;
}
