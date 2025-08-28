namespace FcaAssistant.Ha.Entities;

public interface IHaSetEntity : IHaEntity
{
    Task OnSetAsync(string state);
}