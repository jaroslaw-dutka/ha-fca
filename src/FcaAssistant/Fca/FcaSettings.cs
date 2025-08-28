using FcaAssistant.Fca.Model;

namespace FcaAssistant.Fca;

public record FcaSettings
{
    public FcaBrand Brand { get; set; }

    public FcaRegion Region { get; set; }

    public string User { get; set; }

    public string Password { get; set; }

    public string? Pin { get; set; }
}