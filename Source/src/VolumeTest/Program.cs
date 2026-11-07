using FMOD;
using System.Runtime.InteropServices;
class Program {
 [DllImport("libopenal.so.1",CallingConvention=CallingConvention.Cdecl)] static extern void alGetListenerf(int key,out float value);
 static void Check(bool condition,string message) { if(!condition)throw new Exception(message); }
 static void Near(float actual,float expected,string message) => Check(Math.Abs(actual-expected)<0.000001,message+": "+actual+" != "+expected);
 static int Main() {
  var dir=Path.Combine(Path.GetTempPath(),"bastion-volume-test-"+Guid.NewGuid());Directory.CreateDirectory(dir);var node=Path.Combine(dir,"volume");
  try {
   var input=Path.Combine(dir,"input-event");var packet=new byte[24];packet[16]=1;packet[18]=115;packet[20]=1;File.WriteAllBytes(input,packet);
   var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static;
   int fd=(int)typeof(RGDSVolume).GetMethod("NativeOpen",flags)!.Invoke(null,new object[]{input,0x80800})!;Check(fd>=0,"native input open ABI");
   try { var received=new byte[24];long size=(long)typeof(RGDSVolume).GetMethod("NativeRead",flags)!.Invoke(null,new object[]{fd,received,(nuint)24})!;Check(size==24 && received.SequenceEqual(packet),"native input read ABI"); }
   finally {typeof(RGDSVolume).GetMethod("NativeClose",flags)!.Invoke(null,new object[]{fd});}
   foreach(int initial in Enumerable.Range(0,11)) {
    File.WriteAllText(node,initial+"\n");RGDSVolume.InitializeCore(node,false);
    Near(RGDSVolume.Gain,(float)(0.35*Math.Pow(initial/10.0,2)),"entry curve");
    Framework.init();alGetListenerf(0x100A,out float gain);Near(gain,RGDSVolume.Curve(initial),"OpenAL gain before any sound");
    RGDSVolume.HandleEvent(1,115,1,1000,5);Check(RGDSVolume.Level==Math.Min(initial+1,10),"volume up");
    RGDSVolume.HandleEvent(1,115,2,1100,5);Check(RGDSVolume.Level==Math.Min(initial+1,10),"repeat throttle");
    RGDSVolume.HandleEvent(1,115,2,1200,5);Check(RGDSVolume.Level==Math.Min(initial+2,10),"held volume key");
    RGDSVolume.HandleEvent(1,114,1,1300,5);int before=RGDSVolume.Level;
    RGDSVolume.HandleEvent(3,114,1,1400,5);RGDSVolume.HandleEvent(1,114,0,1400,5);Check(RGDSVolume.Level==before,"axis/release ignored");
    Thread.Sleep(50);alGetListenerf(0x100A,out gain);Near(gain,RGDSVolume.Gain,"live OpenAL gain follows keys");
    Framework.resetSystem();Check(File.ReadAllText(node).Trim()==before.ToString(),"audio reset preserves session volume");
    Framework.terminate();RGDSVolume.Shutdown();Check(File.ReadAllText(node).Trim()==initial.ToString(),"OS level restored");
   }
   File.WriteAllText(node,"invalid\n");RGDSVolume.InitializeCore(node,false);Near(RGDSVolume.Gain,0.014f,"safe fallback");RGDSVolume.HandleEvent(1,115,1,1000,5);RGDSVolume.Shutdown();Check(File.ReadAllText(node)=="invalid\n","invalid node is not overwritten");
   Console.WriteLine("PASS: all 11 OS entry levels; exact curve; initial and live OpenAL master gain; physical key/repeat logic; audio reset; exact OS restoration; safe invalid-node fallback.");return 0;
  } finally { Framework.terminate();RGDSVolume.Shutdown();Directory.Delete(dir,true); }
 }
}
