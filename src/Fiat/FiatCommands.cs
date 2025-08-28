using FiatChamp.Fiat.Model;

namespace FiatChamp.Fiat;

public class FiatCommands
{
    public static readonly FiatCommand ChargeNow = new() { Action = "ev/chargenow", Message = "CNOW" };
    public static readonly FiatCommand DeepRefresh = new() { Action = "ev", Message = "DEEPREFRESH" };
    public static readonly FiatCommand VehicleFinder = new() { Action = "location", Message = "VF" };

    public static readonly FiatCommand Blink = new() { Message = "ROLIGHTS" };
    
    public static readonly FiatCommand ClimateOn = new() { Message = "ROPRECOND" };
    public static readonly FiatCommand ClimateOff = new() { Message = "ROPRECOND_OFF", IsDangerous = true };

    public static readonly FiatCommand TrunkUnlock = new() { Message = "ROTRUNKUNLOCK" };
    public static readonly FiatCommand TrunkLock = new() { Message = "ROTRUNKLOCK" };

    public static readonly FiatCommand DoorsUnlock = new() { Message = "RDU", IsDangerous = true };
    public static readonly FiatCommand DoorsLock = new() { Message = "RDL", IsDangerous = true };
}