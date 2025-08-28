namespace FcaAssistant.Fca.Model;

public class FcaCommand
{
    public bool IsDangerous { get; set; }
    public required string Message { get; init; }
    public string Action { get; init; } = "remote";
}