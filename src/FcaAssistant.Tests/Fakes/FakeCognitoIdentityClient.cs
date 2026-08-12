using Amazon;
using Amazon.CognitoIdentity;
using Amazon.CognitoIdentity.Model;
using Amazon.Runtime;

namespace FcaAssistant.Tests.Fakes;

/// <summary>
/// Subclasses the real client and overrides only the single operation <see cref="FcaAssistant.Fca.FcaLiveClient"/>
/// uses, so the AWS credential exchange can be exercised offline.
/// </summary>
public class FakeCognitoIdentityClient() : AmazonCognitoIdentityClient(new AnonymousAWSCredentials(), RegionEndpoint.EUWest1)
{
    public Credentials Credentials { get; set; } = new()
    {
        AccessKeyId = "access-key",
        SecretKey = "secret-key",
        SessionToken = "session-token"
    };

    public override Task<GetCredentialsForIdentityResponse> GetCredentialsForIdentityAsync(
        GetCredentialsForIdentityRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new GetCredentialsForIdentityResponse { Credentials = Credentials });
}
