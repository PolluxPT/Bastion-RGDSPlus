using System.Runtime.InteropServices;
using BastionRGDS;
using System.Text.Json;
var profile=JsonSerializer.Deserialize<ControlProfile>(File.ReadAllText(args[0]))!;
Environment.SetEnvironmentVariable("BASTION_CONTROLLER_PROFILE",args[0]);
void Check(bool yes,string msg){if(!yes)throw new Exception(msg);}
Check(profile.Valid(profile.Guid,17,4,1),"Valid profile rejected");Check(!profile.Valid(profile.Guid,16,4,1),"Wrong button count accepted");Check(!profile.Valid(new string('0',32),17,4,1),"Wrong controller accepted");
string original=File.ReadAllText(args[0]);var broken=JsonSerializer.Deserialize<ControlProfile>(original)!;broken.Bindings["a"]=broken.Bindings["b"];Check(!broken.Valid(profile.Guid,17,4,1),"Duplicate accepted");try{broken.Save();throw new Exception("Invalid profile saved");}catch(InvalidDataException){}Check(File.ReadAllText(args[0])==original,"Invalid profile replaced saved data");
N.SDL_InitSubSystem(0x2200);int index=N.SDL_JoystickAttachVirtual(1,4,17,1);Check(index>=0,"Virtual controller missing");byte[] guid=new byte[33];N.SDL_JoystickGetGUIDString(N.SDL_JoystickGetDeviceGUID(index),guid,33);Check(System.Text.Encoding.ASCII.GetString(guid,0,32)==profile.Guid,"Virtual GUID differs");Check(N.SDL_GameControllerAddMapping(profile.Guid+",WRONG,a:b0,b:b1,back:b6,start:b7,platform:Linux,")>=0,"Baseline mapping rejected");BastionRGDS.Device.ConfigureControllers();Check(BastionRGDS.Device.HasCalibratedControls,"Learned profile did not override stale mapping");BastionRGDS.HardwareTriggers.Initialize();var joystick=N.SDL_JoystickOpen(index);var controller=N.SDL_GameControllerOpen(index);Check(controller!=IntPtr.Zero,"Controller failed to open");
var enums=new Dictionary<string,int>{{"a",0},{"b",1},{"x",2},{"y",3},{"back",4},{"start",6},{"leftshoulder",9},{"rightshoulder",10},{"dpup",11},{"dpdown",12},{"dpleft",13},{"dpright",14}};
void Clear(){for(int b=0;b<17;b++)N.SDL_JoystickSetVirtualButton(joystick,b,0);N.SDL_JoystickSetVirtualHat(joystick,0,0);}
void Press(string action){string v=profile.Bindings[action];if(v[0]=='b')N.SDL_JoystickSetVirtualButton(joystick,int.Parse(v[1..]),1);else N.SDL_JoystickSetVirtualHat(joystick,int.Parse(v[1..].Split('.')[0]),byte.Parse(v.Split('.')[1]));}
foreach(var step in ControlProfile.Steps){Clear();Press(step.Action);N.SDL_GameControllerUpdate();foreach(var item in enums)Check((N.SDL_GameControllerGetButton(controller,item.Value)!=0)==(item.Key==step.Action),"Wrong logical button for "+step.Action+" -> "+item.Key);Check((N.SDL_GameControllerGetAxis(controller,4)>16000)==(step.Action=="lefttrigger"),"LT mismatch");Check((N.SDL_GameControllerGetAxis(controller,5)>16000)==(step.Action=="righttrigger"),"RT mismatch");Clear();N.SDL_GameControllerUpdate();Check(N.SDL_GameControllerGetAxis(controller,4)==0&&N.SDL_GameControllerGetAxis(controller,5)==0,"Trigger release mismatch");}
Clear();Press("start");Press("back");N.SDL_GameControllerUpdate();Check(N.SDL_GameControllerGetButton(controller,4)!=0&&N.SDL_GameControllerGetButton(controller,6)!=0,"START SELECT combo mismatch");
Console.WriteLine("PASS: learned 17-button permutation: all 14 logical controls, independent press/release, digital LT/RT and START+SELECT; no duplicate actions. GUID/count validation and atomic preservation of previous profile passed.");N.SDL_Quit();
static class N {
 const string L="libSDL2-2.0.so.0";
 [StructLayout(LayoutKind.Sequential)]public struct G{public ulong a,b;}
 [DllImport(L)]public static extern int SDL_InitSubSystem(uint f);
 [DllImport(L)]public static extern int SDL_JoystickAttachVirtual(int t,int a,int b,int h);
 [DllImport(L)]public static extern IntPtr SDL_JoystickOpen(int i);
 [DllImport(L)]public static extern G SDL_JoystickGetDeviceGUID(int i);
 [DllImport(L)]public static extern void SDL_JoystickGetGUIDString(G g,byte[] s,int n);
 [DllImport(L)]public static extern int SDL_GameControllerAddMapping(string s);
 [DllImport(L)]public static extern IntPtr SDL_GameControllerOpen(int i);
 [DllImport(L)]public static extern int SDL_JoystickSetVirtualButton(IntPtr j,int b,byte v);
 [DllImport(L)]public static extern int SDL_JoystickSetVirtualHat(IntPtr j,int h,byte v);
 [DllImport(L)]public static extern byte SDL_GameControllerGetButton(IntPtr c,int b);
 [DllImport(L)]public static extern short SDL_GameControllerGetAxis(IntPtr c,int a);
 [DllImport(L)]public static extern void SDL_GameControllerUpdate();
 [DllImport(L)]public static extern void SDL_Quit();
}
