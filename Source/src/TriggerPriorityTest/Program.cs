using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Runtime.InteropServices;
using System.Reflection;
const string guid="ff0013db5669727475616c2043007601";
string mapping=guid+",CALIBRATED,a:b0,b:b1,x:b3,y:b2,leftshoulder:b4,rightshoulder:b5,back:b6,start:b7,lefttrigger:b10,righttrigger:b11,dpup:h0.1,dpdown:h0.4,dpleft:h0.8,dpright:h0.2,leftx:a0,lefty:a1,platform:Linux,";
N.SDL_InitSubSystem(0x2200);int index=N.SDL_JoystickAttachVirtual(1,4,17,1);if(index<0)throw new Exception("No virtual joystick");IntPtr j=N.SDL_JoystickOpen(index);N.SDL_GameControllerAddMapping(mapping);
FNALoggerEXT.LogInfo=Console.WriteLine;FNALoggerEXT.LogWarn=Console.WriteLine;FNALoggerEXT.LogError=Console.WriteLine;

bool fixedEnv=args[0]=="fixed";
void Check(bool yes,string why){if(!yes)throw new Exception(why);}
void Press(int b,byte v){N.SDL_JoystickSetVirtualButton(j,b,v);N.SDL_GameControllerUpdate();}
var caps=GamePad.GetCapabilities(PlayerIndex.One);Check(caps.IsConnected,"Controller absent");Check(caps.HasLeftTrigger==fixedEnv&&caps.HasRightTrigger==fixedEnv,"Capabilities did not follow effective mapping");
foreach(int button in new[]{10,11}){Press(button,1);var state=GamePad.GetState(PlayerIndex.One);float value=button==10?state.Triggers.Left:state.Triggers.Right;bool down=state.IsButtonDown(button==10?Buttons.LeftTrigger:Buttons.RightTrigger);Check(value==(fixedEnv?1f:0f)&&down==fixedEnv,"FNA trigger state incorrect");Console.WriteLine($"raw b{button}: trigger={value}; digital button={down}; packet={state.PacketNumber}");Press(button,0);state=GamePad.GetState(PlayerIndex.One);Check(state.Triggers.Left==0&&state.Triggers.Right==0&&!state.IsButtonDown(Buttons.LeftTrigger)&&!state.IsButtonDown(Buttons.RightTrigger),"Release stuck");}
Press(6,1);Press(7,1);var combo=GamePad.GetState(PlayerIndex.One);Check(combo.Buttons.Back==ButtonState.Pressed&&combo.Buttons.Start==ButtonState.Pressed,"START SELECT regression");Console.WriteLine(fixedEnv?"PASS: exported mapping reaches FNA LT/RT capabilities, float values, button flags and release; START/SELECT preserved.":"PASS: reproduced old environment overriding AddMapping and hiding both FNA triggers.");
N.SDL_Quit();
static class N{
 const string L="libSDL2-2.0.so.0";
 [DllImport(L)]public static extern int SDL_InitSubSystem(uint f);
 [DllImport(L)]public static extern int SDL_JoystickAttachVirtual(int t,int a,int b,int h);
 [DllImport(L)]public static extern IntPtr SDL_JoystickOpen(int i);
 [DllImport(L)]public static extern int SDL_GameControllerAddMapping(string s);
 [DllImport(L)]public static extern int SDL_JoystickSetVirtualButton(IntPtr j,int b,byte v);
 [DllImport(L)]public static extern void SDL_GameControllerUpdate();
 [DllImport(L)]public static extern void SDL_Quit();
}
