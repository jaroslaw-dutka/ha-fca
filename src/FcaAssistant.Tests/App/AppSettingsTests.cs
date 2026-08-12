using FcaAssistant.App;

namespace FcaAssistant.Tests.App;

public class AppSettingsTests
{
    [Theory]
    [InlineData(DistanceUnit.Miles, "mi")]
    [InlineData(DistanceUnit.Kilometers, "km")]
    public void TargetDistanceUnit_DerivedFromDistanceUnit(DistanceUnit unit, string expected)
    {
        Assert.Equal(expected, new AppSettings { DistanceUnit = unit }.TargetDistanceUnit);
    }
}
