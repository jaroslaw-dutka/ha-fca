using System.Text.Json.Serialization;

namespace FcaAssistant.Fca.Model;

public class FcaEmails
{
    [JsonPropertyName("verified")]
    public List<string> Verified { get; set; }

    [JsonPropertyName("unverified")]
    public List<string> Unverified { get; set; }
}