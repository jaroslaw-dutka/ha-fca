namespace FcaAssistant.App.Mapping;

public interface IVehicleDetailsMapper
{
    SensorPresentation Map(string key, string value, string targetUnit, IReadOnlyDictionary<string, string> properties);
}
