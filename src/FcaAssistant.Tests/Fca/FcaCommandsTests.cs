using FcaAssistant.Fca;

namespace FcaAssistant.Tests.Fca;

public class FcaCommandsTests
{
    [Fact]
    public void ClimateOn_IsRoprecond_AndSafe()
    {
        Assert.Equal("ROPRECOND", FcaCommands.ClimateOn.Message);
        Assert.False(FcaCommands.ClimateOn.IsDangerous);
    }

    [Fact]
    public void ClimateOff_IsRoprecondOff_AndDangerous()
    {
        Assert.Equal("ROPRECOND_OFF", FcaCommands.ClimateOff.Message);
        Assert.True(FcaCommands.ClimateOff.IsDangerous);
    }

    [Theory]
    [MemberData(nameof(DangerousCommands))]
    public void DangerousCommands_AreFlagged(FcaAssistant.Fca.Model.FcaCommand command)
    {
        Assert.True(command.IsDangerous);
    }

    public static IEnumerable<object[]> DangerousCommands() =>
    [
        [FcaCommands.ClimateOff],
        [FcaCommands.DoorsUnlock],
        [FcaCommands.DoorsLock]
    ];

    [Fact]
    public void CommandsWithoutExplicitAction_DefaultToRemote()
    {
        Assert.Equal("remote", FcaCommands.Blink.Action);
        Assert.Equal("remote", FcaCommands.ClimateOn.Action);
    }

    [Fact]
    public void ChargeNow_TargetsChargeNowAction()
    {
        Assert.Equal("ev/chargenow", FcaCommands.ChargeNow.Action);
        Assert.Equal("CNOW", FcaCommands.ChargeNow.Message);
    }

    [Fact]
    public void SafeCommands_AreNotFlaggedDangerous()
    {
        Assert.False(FcaCommands.Blink.IsDangerous);
        Assert.False(FcaCommands.ChargeNow.IsDangerous);
        Assert.False(FcaCommands.TrunkUnlock.IsDangerous);
    }
}
