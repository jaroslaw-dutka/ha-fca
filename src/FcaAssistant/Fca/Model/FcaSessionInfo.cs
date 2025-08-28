using System.Text.Json.Serialization;

namespace FcaAssistant.Fca.Model;

public class FcaSessionInfo
{
    [JsonPropertyName("login_token")]
    public string LoginToken { get; set; }

    [JsonPropertyName("expires_in")]
    public string ExpiresIn { get; set; }
}