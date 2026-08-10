using FcaAssistant.Ha.Model;

namespace FcaAssistant.Ha.Entities;

public class HaButton(HaDevice device, string name, Func<HaButton, string, Task> setAction)
    : HaEntity(device, "button", name), IHaSetEntity
{
    public virtual async Task OnSetAsync(string state) =>
        await setAction(this, state);
}