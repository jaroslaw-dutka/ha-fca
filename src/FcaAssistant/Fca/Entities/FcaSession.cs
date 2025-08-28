using Amazon.Runtime;

namespace FcaAssistant.Fca.Entities;

public class FcaSession
{
    public string UserId { get; set; }
    public ImmutableCredentials AwsCredentials { get; set; }
}