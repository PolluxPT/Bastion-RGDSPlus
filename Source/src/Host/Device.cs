using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
namespace BastionRGDS;
public static class Device {
 public static bool HasCalibratedControls {get;private set;}
 const string SDL="libSDL2-2.0.so.0";
 static IntPtr testJoystick;
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate uint GetGLError();
 [DllImport(SDL,CallingConvention=CallingConvention.Cdecl)] static extern IntPtr SDL_GL_GetProcAddress(string name);
 static GetGLError glError; static int glWarnings;
 [DllImport(SDL,CallingConvention=CallingConvention.Cdecl)] static extern int SDL_JoystickAttachVirtual(int type,int axes,int buttons,int hats);
 [DllImport(SDL,CallingConvention=CallingConvention.Cdecl)] static extern IntPtr SDL_JoystickOpen(int index);
 [DllImport(SDL,CallingConvention=CallingConvention.Cdecl)] static extern int SDL_JoystickNumButtons(IntPtr joystick);
 [DllImport(SDL,CallingConvention=CallingConvention.Cdecl)] static extern int SDL_JoystickNumAxes(IntPtr joystick);
 [DllImport(SDL,CallingConvention=CallingConvention.Cdecl)] static extern int SDL_JoystickNumHats(IntPtr joystick);
 [DllImport(SDL,CallingConvention=CallingConvention.Cdecl)] static extern void SDL_JoystickClose(IntPtr joystick);
 [DllImport(SDL,CallingConvention=CallingConvention.Cdecl)] static extern int SDL_JoystickSetVirtualButton(IntPtr joystick,int button,byte value);
 static long heldSince,lastLog; static int samples,totalFrames,drawSamples; static bool exiting,captured,testKeyDown; static List<GameComponent> blooms; static readonly long born=Stopwatch.GetTimestamp();
 [StructLayout(LayoutKind.Sequential)] struct Guid16 { public ulong lo,hi; }
 [DllImport(SDL,CallingConvention=CallingConvention.Cdecl)] static extern int SDL_InitSubSystem(uint flags);
 [DllImport(SDL,CallingConvention=CallingConvention.Cdecl)] static extern int SDL_NumJoysticks();
 [DllImport(SDL,CallingConvention=CallingConvention.Cdecl)] static extern int SDL_PushEvent(byte[] data);
 [DllImport(SDL,CallingConvention=CallingConvention.Cdecl)] static extern uint SDL_GetWindowID(IntPtr window);
 [DllImport(SDL,CallingConvention=CallingConvention.Cdecl)] static extern IntPtr SDL_JoystickNameForIndex(int i);
 [DllImport(SDL,CallingConvention=CallingConvention.Cdecl)] static extern Guid16 SDL_JoystickGetDeviceGUID(int i);
 [DllImport(SDL,CallingConvention=CallingConvention.Cdecl)] static extern void SDL_JoystickGetGUIDString(Guid16 guid,byte[] result,int length);
 [DllImport(SDL,CallingConvention=CallingConvention.Cdecl)] static extern int SDL_GameControllerAddMapping([MarshalAs(UnmanagedType.LPUTF8Str)]string mapping);
 public static void ConfigureControllers() {
  if(SDL_InitSubSystem(0x2000)!=0) { Console.WriteLine("Controller subsystem init failed; FNA will retry");return; }
  if(Environment.GetEnvironmentVariable("BASTION_TEST_INPUT")=="1") { int index=SDL_JoystickAttachVirtual(1,6,15,1);if(index>=0)testJoystick=SDL_JoystickOpen(index); }
  for(int i=0;i<SDL_NumJoysticks();i++) {
   var name=Marshal.PtrToStringUTF8(SDL_JoystickNameForIndex(i))??"unknown";
   byte[] bytes=new byte[33]; SDL_JoystickGetGUIDString(SDL_JoystickGetDeviceGUID(i),bytes,bytes.Length);
   string guid=System.Text.Encoding.ASCII.GetString(bytes,0,32);
   Console.WriteLine("Controller "+i+": "+name+" / "+guid);
   ControlProfile profile=null;var raw=SDL_JoystickOpen(i);if(raw!=IntPtr.Zero){int nb=SDL_JoystickNumButtons(raw),na=SDL_JoystickNumAxes(raw),nh=SDL_JoystickNumHats(raw);Console.WriteLine($"Raw controller: buttons={nb} axes={na} hats={nh}");profile=ControlProfile.Load(guid,nb,na,nh);SDL_JoystickClose(raw);}
   if(profile!=null || name.Contains("ANBERNIC-rk3568",StringComparison.OrdinalIgnoreCase)) {
    // Verified SDL layout used by PolluxPT Braid, Sonic Mania and SteamWorld Dig ports.
    string mapping=guid+",RGDS+ native,a:b0,b:b1,x:b3,y:b2,back:b6,start:b7,leftshoulder:b4,rightshoulder:b5,leftx:a0,lefty:a1,dpup:h0.1,dpright:h0.2,dpdown:h0.4,dpleft:h0.8,platform:Linux,";
    var configured=(Environment.GetEnvironmentVariable("SDL_GAMECONTROLLERCONFIG")??"").Split('\n').FirstOrDefault(line=>line.StartsWith(guid+",",StringComparison.OrdinalIgnoreCase));
    if(profile!=null){mapping=profile.Mapping();HasCalibratedControls=true;Console.WriteLine("CONTROLS: applying learned per-GUID calibration: "+mapping);}
    else if(!string.IsNullOrEmpty(configured))mapping=configured.Trim();
    Console.WriteLine("RGDS mapping result: "+SDL_GameControllerAddMapping(mapping));
   }
  }
 }
 public static void AfterDraw(Game game) {
  drawSamples++;
  if(totalFrames<5 || totalFrames%60==0) {
   if(glError==null) { var ptr=SDL_GL_GetProcAddress("glGetError"); if(ptr!=IntPtr.Zero)glError=Marshal.GetDelegateForFunctionPointer<GetGLError>(ptr); }
   if(glError!=null && glWarnings<16)for(int i=0;i<8;i++) { uint error=glError(); if(error==0)break;glWarnings++;Console.WriteLine("GRAPHICS API error: 0x"+error.ToString("X4")+" frame="+totalFrames); }
  }
  string capture=Environment.GetEnvironmentVariable("BASTION_TEST_CAPTURE");
  double.TryParse(Environment.GetEnvironmentVariable("BASTION_TEST_CAPTURE_AFTER"),out double captureAfter);
  if(!captured && (Stopwatch.GetTimestamp()-born)/(double)Stopwatch.Frequency>Math.Max(8,captureAfter) && !string.IsNullOrEmpty(capture)) {
   captured=true;
   try { var pp=game.GraphicsDevice.PresentationParameters; var data=new Color[pp.BackBufferWidth*pp.BackBufferHeight];game.GraphicsDevice.GetBackBufferData(data);
    using var texture=new Microsoft.Xna.Framework.Graphics.Texture2D(game.GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);texture.SetData(data);
    using var f=File.Create(capture);texture.SaveAsPng(f,pp.BackBufferWidth,pp.BackBufferHeight); Console.WriteLine("Test screenshot saved");
   }catch(Exception ex) { Console.WriteLine("Test screenshot: "+ex.Message); }
  }
 }
 public static void OnFrame(Game game) {
  long now=Stopwatch.GetTimestamp();
  BattlePerformance.Configure(game);
  if(totalFrames==0) {
   var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
   Console.WriteLine("Texture package root: "+game.Content.GetType().GetField("m_packageRoot",flags)?.GetValue(game.Content));
  }
  if(Environment.GetEnvironmentVariable("BASTION_BLOOM")=="0") {
   if(blooms==null) { blooms=new List<GameComponent>(); var f=game.GetType().GetField("m_bloomComponents",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
    if(f?.GetValue(game) is System.Collections.IDictionary map)foreach(var value in map.Values)if(value is GameComponent component)blooms.Add(component);
    Console.WriteLine("Bloom disabled: "+blooms.Count+" components");
   }
   foreach(var component in blooms)component.Enabled=false;
  }
  if(double.TryParse(Environment.GetEnvironmentVariable("BASTION_TEST_SECONDS"),out double limit) && (now-born)/(double)Stopwatch.Frequency>=limit)game.Exit();
  totalFrames++;
  if(Environment.GetEnvironmentVariable("BASTION_TEST_INPUT")=="1") {
   double elapsed=(now-born)/(double)Stopwatch.Frequency;
   bool down=elapsed>=16 && elapsed<90 && ((elapsed-16)%6)<0.25;
   if(down!=testKeyDown) { testKeyDown=down; if(testJoystick!=IntPtr.Zero)SDL_JoystickSetVirtualButton(testJoystick,0,(byte)(down?1:0)); byte[] e=new byte[56]; BitConverter.GetBytes((uint)(down?0x300:0x301)).CopyTo(e,0);BitConverter.GetBytes(SDL_GetWindowID(game.Window.Handle)).CopyTo(e,8);e[12]=(byte)(down?1:0);BitConverter.GetBytes(40).CopyTo(e,16);BitConverter.GetBytes(13).CopyTo(e,20);SDL_PushEvent(e); }
  }
  bool combo=false;
  string exitFile=Environment.GetEnvironmentVariable("BASTION_EXIT_REQUEST");
  if(!exiting && !string.IsNullOrEmpty(exitFile) && File.Exists(exitFile)) { exiting=true;Console.WriteLine("Exit requested by hardware guard");game.Exit(); }
  for(int i=0;i<4;i++) { var state=GamePad.GetState((PlayerIndex)i); if(state.IsConnected && state.Buttons.Start==ButtonState.Pressed && state.Buttons.Back==ButtonState.Pressed)combo=true; }
  if(combo) { if(heldSince==0)heldSince=now; if(!exiting && (now-heldSince)/(double)Stopwatch.Frequency>=1.5) { exiting=true;Console.WriteLine("Exit requested: START+SELECT");game.Exit(); } } else heldSince=0;
  samples++;
  if(lastLog==0)lastLog=now;
  if((now-lastLog)/(double)Stopwatch.Frequency>=10) { Console.WriteLine("Update rate "+(samples*Stopwatch.Frequency/(double)(now-lastLog)).ToString("F1")+"/s; draw rate "+(drawSamples*Stopwatch.Frequency/(double)(now-lastLog)).ToString("F1")+"/s; managed memory "+GC.GetTotalMemory(false)); BattlePerformance.Log(game); try { Console.WriteLine(File.ReadLines("/proc/self/status").FirstOrDefault(line=>line.StartsWith("VmRSS:"))); }catch{} lastLog=now;samples=0;drawSamples=0; }
 }
}
