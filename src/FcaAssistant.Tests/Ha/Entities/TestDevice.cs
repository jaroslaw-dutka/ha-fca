using FcaAssistant.Ha.Model;

namespace FcaAssistant.Tests.Ha.Entities;

internal static class TestDevice
{
    public static HaDevice Create(string vin = "VIN123") => new()
    {
        Name = "Car",
        Identifiers = [vin],
        Manufacturer = "Fiat",
        Model = "500e",
        Version = "1.0"
    };
}
