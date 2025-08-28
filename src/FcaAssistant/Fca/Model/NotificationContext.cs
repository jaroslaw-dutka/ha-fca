using System.Text.Json.Serialization;

namespace FcaAssistant.Fca.Model;

public class NotificationContext
{
    [JsonPropertyName("nickname")]
    public string Nickname { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; }

    [JsonPropertyName("brandMarketingName")]
    public string BrandMarketingName { get; set; }

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; }
}