using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using BastionRGDS;
Native.SDL_InitSubSystem(0x2000);int idx=Native.SDL_JoystickAttachVirtual(1,6,15,1);if(idx<0)throw new Exception("Virtual controller missing");byte[] guid=new byte[33];Native.SDL_JoystickGetGUIDString(Native.SDL_JoystickGetDeviceGUID(idx),guid,33);string id=System.Text.Encoding.ASCII.GetString(guid,0,32);Native.SDL_GameControllerAddMapping(id+",RGDS trigger test,a:b0,b:b1,x:b3,y:b2,back:b6,start:b7,leftshoulder:b4,rightshoulder:b5,leftx:a0,lefty:a1,rightx:a2,righty:a3,platform:Linux,");var joy=Native.SDL_JoystickOpen(idx);Native.SDL_JoystickSetVirtualButton(joy,0,1);Native.SDL_JoystickSetVirtualButton(joy,6,1);Native.SDL_JoystickSetVirtualAxis(joy,2,25000);Native.SDL_JoystickSetVirtualAxis(joy,3,-25000);Native.SDL_JoystickUpdate();using var game=new Test();game.Run();if(!game.Passed)throw new Exception("Input test did not finish");
class Test:Game {
 public bool Passed;int frames;readonly byte[] bitmap=new byte[96];
 public Test(){new GraphicsDeviceManager(this){PreferredBackBufferWidth=64,PreferredBackBufferHeight=64};GamePad.RGDSDigitalTriggersEXT=_=>{int m=HardwareTriggers.Decode(bitmap);return new GamePadTriggers((m&1)!=0?1:0,(m&2)!=0?1:0);};}
 protected override void Update(GameTime t){if(++frames<3)return;var caps=GamePad.GetCapabilities(PlayerIndex.One);if(!caps.IsConnected||!caps.HasLeftTrigger||!caps.HasRightTrigger)throw new Exception("Trigger capabilities absent");int? packet=null;foreach(int mask in new[]{0,1,3,2,0}){Array.Clear(bitmap);bitmap[312/8]=(byte)mask;bitmap[114/8]|=(byte)(1<<(114%8));int decoded=HardwareTriggers.Decode(bitmap);if(decoded!=mask)throw new Exception("Evdev bitmap decode");var s=GamePad.GetState(PlayerIndex.One);if(!s.IsConnected||s.Buttons.A!=ButtonState.Pressed||s.Buttons.Back!=ButtonState.Pressed)throw new Exception("Original buttons changed");if((s.Triggers.Left>0.5f)!=((mask&1)!=0)||(s.Triggers.Right>0.5f)!=((mask&2)!=0))throw new Exception("Trigger values incorrect");if(s.IsButtonDown(Buttons.LeftTrigger)!=((mask&1)!=0)||s.IsButtonDown(Buttons.RightTrigger)!=((mask&2)!=0))throw new Exception("Trigger button flags incorrect");if(packet==s.PacketNumber)throw new Exception("Digital transition did not change packet number");packet=s.PacketNumber;}GamePad.RGDSDigitalTriggersEXT=null;var fallback=GamePad.GetState(PlayerIndex.One);if(fallback.Triggers.Left>0.1f||fallback.Triggers.Right>0.1f)throw new Exception("Analogue stick was confused with trigger");MemoryBudget.PackageUnloaded();MemoryBudget.PackageUnloaded();MemoryBudget.BeforePackageLoad();MemoryBudget.BeforePackageLoad();Console.WriteLine("PASS: evdev TL2/TR2 bitmaps -> FNA LT/RT values, button flags, press/release packet numbers, capabilities; A/SELECT/sticks preserved; fallback unchanged. Coalesced package cleanup exercised.");Passed=true;Exit();}
}
static class Native {
 const string L="libSDL2-2.0.so.0";
 [StructLayout(LayoutKind.Sequential)] public struct Guid16{public ulong a,b;}
 [DllImport(L)] public static extern int SDL_InitSubSystem(uint f);
 [DllImport(L)] public static extern int SDL_JoystickAttachVirtual(int type,int axes,int buttons,int hats);
 [DllImport(L)] public static extern IntPtr SDL_JoystickOpen(int i);
 [DllImport(L)] public static extern Guid16 SDL_JoystickGetDeviceGUID(int i);
 [DllImport(L)] public static extern void SDL_JoystickGetGUIDString(Guid16 id,byte[] s,int n);
 [DllImport(L)] public static extern int SDL_GameControllerAddMapping(string s);
 [DllImport(L)] public static extern int SDL_JoystickSetVirtualButton(IntPtr j,int b,byte v);
 [DllImport(L)] public static extern int SDL_JoystickSetVirtualAxis(IntPtr j,int a,short v);
 [DllImport(L)] public static extern void SDL_JoystickUpdate();
}
