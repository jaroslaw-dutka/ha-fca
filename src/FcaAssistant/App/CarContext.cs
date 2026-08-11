using FcaAssistant.Fca.Entities;
using FcaAssistant.Ha.Model;

namespace FcaAssistant.App;

public record CarContext(VehicleInfo Vehicle, IReadOnlyList<HaRestApiEntityState> States)
{
    public HaDevice Device { get; init; } = new()
    {
        Name = string.IsNullOrEmpty(Vehicle.Vehicle.Nickname) ? "Car" : Vehicle.Vehicle.Nickname,
        Identifiers = [Vehicle.Vehicle.Vin],
        Manufacturer = Vehicle.Vehicle.Make,
        Model = Vehicle.Vehicle.ModelDescription,
        Version = "1.0"
    };
}
