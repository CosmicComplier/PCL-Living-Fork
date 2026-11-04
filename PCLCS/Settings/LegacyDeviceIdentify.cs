using System;
using System.Globalization;
using Microsoft.VisualBasic.CompilerServices;
namespace PCLCS;
public sealed class LegacyDeviceIdentifyHost {
    public Func<long> ApplicationStartTick=null!,Milliseconds=null!;
    public Func<ulong> AvailablePhysicalMemory=null!;
    public Func<object?> ReadHardware=null!;
    public Func<string> ReadSeed=null!;
    public Action<string> WriteSeed=null!,Warn=null!;
    public Func<string,string,string> SourceIdentify=null!;
}
public sealed class LegacyDeviceIdentify {
    private readonly LegacyDeviceIdentifyHost host;
    public LegacyDeviceIdentify(LegacyDeviceIdentifyHost host){this.host=host;}
    public string Read() {
        try {
            if(host.ApplicationStartTick()<1)return "0000-0000-0000-0000";
            string? hardware="Unknown";try{hardware=Convert.ToString(host.ReadHardware(),CultureInfo.InvariantCulture);}catch{}
            string seed=host.ReadSeed();if(seed.Length<3){seed=Conversions.ToString(host.Milliseconds())+Conversions.ToString(host.AvailablePhysicalMemory());host.WriteSeed(seed);}
            return host.SourceIdentify(hardware!,seed);
        }catch{host.Warn("无法读取官方 PCL 设备标识。");return "0000-0000-0000-0000";}
    }
}
