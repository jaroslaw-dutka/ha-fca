using FcaAssistant.Ha.Model;

namespace FcaAssistant.Ha.Entities;

public abstract class HaEntity(HaDevice device, string type, string name) : IHaEntity
{
    public HaDevice Device { get; } = device;
    public string Type { get; } = type;
    public string Name { get; } = name;
    public string Id { get; } = $"{device.Identifiers.First()}_{name}";
    public string? Icon { get; set; }
    public string? UnitOfMeasurement { get; set; }
    public string? DeviceClass { get; set; }
}