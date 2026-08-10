using System.Web;
using Amazon.Runtime;
using FcaAssistant.Aws;

namespace FcaAssistant.Tests.Aws;

public class AwsSignerTests
{
    [Fact]
    public void BuildCanonicalRequest_ProducesSigV4Format()
    {
        var query = new Dictionary<string, string> { ["b"] = "2", ["a"] = "1" };
        var headers = new Dictionary<string, string> { ["Host"] = "example.com" };

        var canonical = AwsSigner.BuildCanonicalRequest("get", "/", query, headers, "UNSIGNED-PAYLOAD");

        Assert.Equal("GET\n/\na=1&b=2\nhost:example.com\n\nhost\nUNSIGNED-PAYLOAD", canonical);
    }

    [Fact]
    public void BuildCanonicalRequest_UrlEncodesQueryValues()
    {
        var query = new Dictionary<string, string> { ["x"] = "a b" };
        var headers = new Dictionary<string, string> { ["host"] = "h" };

        var canonical = AwsSigner.BuildCanonicalRequest("GET", "/p", query, headers, "HASH");

        Assert.Equal("GET\n/p\nx=a%20b\nhost:h\n\nhost\nHASH", canonical);
    }

    [Fact]
    public void BuildStringToSign_HasAlgorithmDateScopeAndHash()
    {
        const string scope = "20200501/eu-west-1/iotdata/aws4_request";
        var canonical = "GET\n/\n\nhost:example.com\n\nhost\nUNSIGNED-PAYLOAD";

        var stringToSign = AwsSigner.BuildStringToSign(new DateTime(2020, 5, 1, 12, 0, 0, DateTimeKind.Utc), scope, canonical);

        var lines = stringToSign.Split('\n');
        Assert.Equal(4, lines.Length);
        Assert.Equal("AWS4-HMAC-SHA256", lines[0]);
        Assert.Matches(@"^\d{8}T\d{6}Z$", lines[1]);
        Assert.Equal(scope, lines[2]);
        Assert.Matches("^[0-9a-f]{64}$", lines[3]);
    }

    [Fact]
    public void SignQuery_AddsSigV4QueryParameters()
    {
        var signed = Sign(new ImmutableCredentials("AKIDEXAMPLE", "SECRET", "SESSIONTOKEN"));

        Assert.Equal("host.example.com", signed.Host);
        Assert.Equal("/mqtt", signed.AbsolutePath);

        var query = HttpUtility.ParseQueryString(signed.Query);
        Assert.Equal("AWS4-HMAC-SHA256", query["X-Amz-Algorithm"]);
        Assert.StartsWith("AKIDEXAMPLE/", query["X-Amz-Credential"]);
        Assert.Equal("host", query["X-Amz-SignedHeaders"]);
        Assert.Matches(@"^\d{8}T\d{6}Z$", query["X-Amz-Date"]);
        Assert.Matches("^[0-9a-f]{64}$", query["X-Amz-Signature"]);
    }

    [Fact]
    public void SignQuery_WithSessionToken_IncludesSecurityToken()
    {
        var signed = Sign(new ImmutableCredentials("AK", "SK", "SESSIONTOKEN"));

        var query = HttpUtility.ParseQueryString(signed.Query);
        Assert.Equal("SESSIONTOKEN", query["X-Amz-Security-Token"]);
    }

    [Fact]
    public void SignQuery_WithoutSessionToken_OmitsSecurityToken()
    {
        var signed = Sign(new ImmutableCredentials("AK", "SK", null));

        var query = HttpUtility.ParseQueryString(signed.Query);
        Assert.Null(query["X-Amz-Security-Token"]);
    }

    [Fact]
    public void SignQuery_IsDeterministicForSameInputs()
    {
        var creds = new ImmutableCredentials("AK", "SK", "TOKEN");

        Assert.Equal(Sign(creds).AbsoluteUri, Sign(creds).AbsoluteUri);
    }

    private static Uri Sign(ImmutableCredentials credentials) => AwsSigner.SignQuery(
        credentials,
        "GET",
        new Uri("https://host.example.com/mqtt"),
        new DateTime(2020, 5, 1, 12, 0, 0, DateTimeKind.Utc),
        "eu-west-1",
        "iotdata");
}
