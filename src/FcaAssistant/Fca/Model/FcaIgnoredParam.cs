using System.Text.Json.Serialization;

namespace FcaAssistant.Fca.Model;

public class FcaIgnoredParam
{
    [JsonPropertyName("paramName")]
    public string ParamName { get; set; }

    [JsonPropertyName("warningCode")]
    public int WarningCode { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }
}