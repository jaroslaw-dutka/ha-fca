namespace FcaAssistant.Ha.Entities;

public interface IHaStateEntity : IHaSetEntity
{
    string State { get; }
}