using System.Text.Json.Serialization;

namespace FcaAssistant.Fca.Model;

public class FcaIdentityResponse : IFcaResponse
{
    [JsonPropertyName("IdentityId")]
    public string IdentityId { get; set; }

    [JsonPropertyName("Token")]
    public string Token { get; set; }

    public bool CheckForError() => string.IsNullOrEmpty(Token) || string.IsNullOrEmpty(IdentityId);

    public void ThrowOnError(string message)
    {
        if (CheckForError())
            throw new Exception(message);
    }
}