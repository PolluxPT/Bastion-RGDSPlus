// RGDS+ private port integration, MIT. Read current key state without grabbing input.
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
namespace BastionRGDS;
public static class HardwareTriggers {
 const ulong GetKeys=0x80604518; // EVIOCGKEY(96), same on x64 and ARM64.
 static int fd=-1,lastMask=-1,messages;
 static readonly byte[] keys=new byte[96];
 [DllImport("libc.so.6",EntryPoint="open",SetLastError=true)] static extern int Open(string path,int flags);
 [DllImport("libc.so.6",EntryPoint="ioctl",SetLastError=true)] static extern int Ioctl(int file,ulong request,[Out] byte[] buffer);
 [DllImport("libc.so.6",EntryPoint="close")] static extern int Close(int file);
 public static int Decode(byte[] bitmap) => ((bitmap[312/8] & (1<<(312%8)))!=0?1:0) | ((bitmap[313/8] & (1<<(313%8)))!=0?2:0);
 public static void Initialize() {
  if(Device.HasCalibratedControls){Console.WriteLine("TRIGGERS: using calibrated SDL LT/RT bindings");return;}
  try {
   foreach(var path in Directory.EnumerateDirectories("/sys/class/input","event*")) {
    if(!File.ReadAllText(Path.Combine(path,"device/name")).Contains("ANBERNIC",StringComparison.OrdinalIgnoreCase))continue;
    int candidate=Open("/dev/input/"+Path.GetFileName(path),0x80800);
    if(candidate<0)continue;
    if(Ioctl(candidate,GetKeys,keys)<0){Close(candidate);continue;}
    fd=candidate;GamePad.RGDSDigitalTriggersEXT=Read;
    Console.WriteLine("TRIGGERS: "+Path.GetFileName(path)+" BTN_TL2(312) -> LT; BTN_TR2(313) -> RT; read-only current-key-state input");
    return;
   }
  }catch(Exception ex) when(ex is IOException || ex is UnauthorizedAccessException) { Console.WriteLine("TRIGGERS discovery: "+ex.Message); }
  Console.WriteLine("TRIGGERS: no accessible ANBERNIC input node; normal SDL mapping retained");
 }
 static GamePadTriggers Read(PlayerIndex player) {
  if(player!=PlayerIndex.One || fd<0 || Ioctl(fd,GetKeys,keys)<0)return default;
  int mask=Decode(keys);
  if(mask!=lastMask){lastMask=mask;if(messages++<48)Console.WriteLine($"TRIGGERS: LT={((mask&1)!=0?1:0)} RT={((mask&2)!=0?1:0)}");}
  return new GamePadTriggers((mask&1)!=0?1:0,(mask&2)!=0?1:0);
 }
 public static void Shutdown() { GamePad.RGDSDigitalTriggersEXT=null;if(fd>=0)Close(fd);fd=-1; }
}
