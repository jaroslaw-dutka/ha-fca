using FcaAssistant.Fca;
using FcaAssistant.Fca.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FcaAssistant.Tests.Fca;

public class FcaApiConfigProviderTests
{
    private static FcaApiConfig Config(FcaBrand brand, FcaRegion region = FcaRegion.Europe) =>
        new FcaApiConfigProvider(
            NullLogger<FcaApiConfigProvider>.Instance,
            Options.Create(new FcaSettings { Brand = brand, Region = region })).Get();

    [Theory]
    [InlineData(FcaBrand.Fiat, FcaRegion.Europe, "eu-west-1")]
    [InlineData(FcaBrand.Fiat, FcaRegion.America, "us-east-1")]
    [InlineData(FcaBrand.Jeep, FcaRegion.Europe, "eu-west-1")]
    [InlineData(FcaBrand.Jeep, FcaRegion.America, "us-east-1")]
    [InlineData(FcaBrand.Ram, FcaRegion.Europe, "us-east-1")]
    [InlineData(FcaBrand.Dodge, FcaRegion.America, "us-east-1")]
    public void Get_MapsBrandAndRegionToAwsEndpoint(FcaBrand brand, FcaRegion region, string expectedRegion) =>
        Assert.Equal(expectedRegion, Config(brand, region).AwsEndpoint.SystemName);

    [Fact]
    public void Get_FiatEurope_UsesEuropeanEndpoints()
    {
        var config = Config(FcaBrand.Fiat, FcaRegion.Europe);

        Assert.Equal("https://loginmyuconnect.fiat.com", config.LoginUrl);
        Assert.Equal("de_de", config.Locale);
    }

    [Theory]
    [InlineData(FcaBrand.AlfaRomeo)]
    [InlineData(FcaBrand.Mock)]
    public void Get_UnsupportedBrand_Throws(FcaBrand brand) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Config(brand));

    [Fact]
    public void Get_ReturnsCachedInstance()
    {
        var provider = new FcaApiConfigProvider(
            NullLogger<FcaApiConfigProvider>.Instance,
            Options.Create(new FcaSettings { Brand = FcaBrand.Fiat, Region = FcaRegion.Europe }));

        Assert.Same(provider.Get(), provider.Get());
    }
}
