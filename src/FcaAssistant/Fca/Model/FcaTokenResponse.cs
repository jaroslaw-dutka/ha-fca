using System.Text.Json.Serialization;

namespace FcaAssistant.Fca.Model;

public class FcaTokenResponse : FcaResponse
{
    [JsonPropertyName("id_token")] 
    public string IdToken { get; set; }
}