using Amazon.Runtime;
using FcaAssistant.Fca;
using FcaAssistant.Fca.Entities;
using FcaAssistant.Fca.Model;
using FcaAssistant.Tests.Fakes;
using Flurl.Http.Configuration;
using Flurl.Http.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FcaAssistant.Tests.Fca;

public class FcaApiClientTests
{
    private static readonly FcaSession Session = new()
    {
        UserId = "user1",
        AwsCredentials = new ImmutableCredentials("access-key", "secret-key", "session-token")
    };

    // The client resolves its IFlurlClient from the cache in its constructor, so it must be built
    // inside the active HttpTest scope for calls to be intercepted.
    private static FcaApiClient CreateClient() => new(
        NullLogger<FcaApiClient>.Instance,
        Options.Create(new FcaSettings { User = "john@example.com", Password = "secret" }),
        new FakeFcaApiConfigProvider(),
        new FlurlClientCache());

    [Fact]
    public async Task Bootstrap_GetsLoginEndpointWithApiKey()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new FcaBootstrapResponse());

        await CreateClient().Bootstrap();

        httpTest.ShouldHaveCalled("https://login.example.com/accounts.webSdkBootstrap")
            .WithVerb(HttpMethod.Get)
            .WithQueryParam("apiKey", "login-api-key");
    }

    [Fact]
    public async Task Login_PostsCredentialsAndDeserializesResponse()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new FcaLoginResponse { UID = "uid-123" });

        var response = await CreateClient().Login();

        Assert.Equal("uid-123", response.UID);
        httpTest.ShouldHaveCalled("https://login.example.com/accounts.login")
            .WithVerb(HttpMethod.Post)
            .WithRequestBody("*loginID=john*");
    }

    [Fact]
    public async Task GetVehicles_TargetsVehiclesEndpointAndParsesList()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new VehicleResponse { Vehicles = [new Vehicle { Vin = "VIN-XYZ" }] });

        var response = await CreateClient().GetVehicles(Session);

        Assert.Equal("VIN-XYZ", Assert.Single(response.Vehicles).Vin);
        httpTest.ShouldHaveCalled("https://api.example.com/v4/accounts/user1/vehicles")
            .WithVerb(HttpMethod.Get)
            .WithQueryParam("stage", "ALL")
            .WithHeader("x-api-key", "api-key");
    }

    [Fact]
    public async Task SendCommand_PostsToActionEndpointAndParsesResponse()
    {
        var correlationId = Guid.NewGuid();
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new FcaCommandResponse { CorrelationId = correlationId });

        var response = await CreateClient().SendCommand(Session, "pin-token", "VIN-XYZ", "remote", "ROLIGHTS");

        Assert.Equal(correlationId, response.CorrelationId);
        httpTest.ShouldHaveCalled("https://api.example.com/v1/accounts/user1/vehicles/VIN-XYZ/remote")
            .WithVerb(HttpMethod.Post)
            .WithHeader("x-api-key", "api-key");
    }
}
