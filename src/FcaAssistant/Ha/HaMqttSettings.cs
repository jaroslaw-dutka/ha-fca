namespace FcaAssistant.Ha;

public class HaMqttSettings
{
    public string ClientId { get; set; }
    public string Server { get; set; }
    public int Port { get; set; }
    public bool UseTls { get; set; }
    public string User { get; set; }
    public string Password { get; set; }
}