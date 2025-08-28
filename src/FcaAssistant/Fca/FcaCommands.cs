using FcaAssistant.Fca.Model;

namespace FcaAssistant.Fca;

public class FcaCommands
{
    public static readonly FcaCommand ChargeNow = new() { Action = "ev/chargenow", Message = "CNOW" };
    public static readonly FcaCommand DeepRefresh = new() { Action = "ev", Message = "DEEPREFRESH" };
    public static readonly FcaCommand VehicleFinder = new() { Action = "location", Message = "VF" };

    public static readonly FcaCommand Blink = new() { Message = "ROLIGHTS" };
    
    public static readonly FcaCommand ClimateOn = new() { Message = "ROPRECOND" };
    public static readonly FcaCommand ClimateOff = new() { Message = "ROPRECOND_OFF", IsDangerous = true };

    public static readonly FcaCommand TrunkUnlock = new() { Message = "ROTRUNKUNLOCK" };
    public static readonly FcaCommand TrunkLock = new() { Message = "ROTRUNKLOCK" };

    public static readonly FcaCommand DoorsUnlock = new() { Message = "RDU", IsDangerous = true };
    public static readonly FcaCommand DoorsLock = new() { Message = "RDL", IsDangerous = true };
}