using FcaAssistant.Ha.Entities;

namespace FcaAssistant.Ha;

public interface IHaEntityPublisher
{
    Task RegisterAsync(IHaSetEntity entity);

    Task PublishAsync(IHaEntity entity);

    T? Get<T>(string id) where T : class, IHaEntity;
}
