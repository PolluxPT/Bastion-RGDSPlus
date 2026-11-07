using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;
using var game=new Probe(args);game.Run();if(!game.Passed)throw new Exception("Probe did not complete");
class Probe:Game {
 readonly GraphicsDeviceManager manager; readonly string[] args; SpriteBatch batch=null!;Texture2D icon=null!,large=null!;Color[] expected=null!;bool captured; public bool Passed;
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate uint GetError();
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void GetTexLevel(uint target,int level,uint name,out int value);
 [DllImport("libSDL2-2.0.so.0")] static extern IntPtr SDL_GL_GetProcAddress(string name);
 public Probe(string[] args) {this.args=args;manager=new GraphicsDeviceManager(this){PreferredBackBufferWidth=384,PreferredBackBufferHeight=160,SynchronizeWithVerticalRetrace=false};Content.RootDirectory=args[0];}
 protected override void LoadContent() {
  batch=new SpriteBatch(GraphicsDevice);icon=Content.Load<Texture2D>("icon");
  if(icon.Format!=SurfaceFormat.Color)throw new Exception("Legacy icon did not convert to RGBA");
  var raw=File.ReadAllBytes(args[1]);expected=new Color[icon.Width*icon.Height];
  for(int i=0;i<expected.Length;i++)expected[i]=new Color(raw[i*4+2],raw[i*4+1],raw[i*4],raw[i*4+3]);
  large=new Texture2D(GraphicsDevice,512,512);var pixels=new Color[512*512];
  for(int y=0;y<512;y++)for(int x=0;x<512;x++)pixels[y*512+x]=x<256?(y<256?Color.Red:Color.Blue):(y<256?Color.Lime:new Color(128,128,128,128));
  large.SetData(pixels);
  var patch=Enumerable.Repeat(new Color(34,51,68,85),27).ToArray();large.SetData(0,new Rectangle(7,9,9,3),patch,0,patch.Length);
 }
 protected override void Draw(GameTime time) {
  if(captured)return;captured=true;GraphicsDevice.Clear(Color.Transparent);
  batch.Begin(SpriteSortMode.Deferred,BlendState.Opaque,SamplerState.PointClamp,DepthStencilState.None,RasterizerState.CullNone);
  batch.Draw(icon,Vector2.Zero,Color.White);batch.Draw(large,new Rectangle(128,0,128,128),Color.White);batch.Draw(large,new Rectangle(300,0,9,3),new Rectangle(7,9,9,3),Color.White);batch.End();
  var get=Marshal.GetDelegateForFunctionPointer<GetTexLevel>(SDL_GL_GetProcAddress("glGetTexLevelParameteriv"));get(0x0DE1,0,0x1003,out int format);
  if(format!=0x8056)throw new Exception("Large texture is not actual RGBA4: "+format.ToString("X"));
  var result=new Color[384*160];GraphicsDevice.GetBackBufferData(result);
  int max=0;for(int y=0;y<icon.Height;y++)for(int x=0;x<icon.Width;x++) {var a=result[y*384+x];var b=expected[y*icon.Width+x];max=Math.Max(max,Delta(a,b));}
  if(max>1)throw new Exception("Saving icon readback differs from original BGRA asset: "+max);
  Check(result[32*384+160],Color.Red,1);Check(result[32*384+224],Color.Lime,1);Check(result[96*384+160],Color.Blue,1);Check(result[96*384+224],new Color(128,128,128,128),17);
  for(int y=0;y<3;y++)for(int x=0;x<9;x++)Check(result[y*384+300+x],new Color(34,51,68,85),17);
  var error=Marshal.GetDelegateForFunctionPointer<GetError>(SDL_GL_GetProcAddress("glGetError"));uint code=error();if(code!=0)throw new Exception("GL error "+code.ToString("X"));
  using var tex=new Texture2D(GraphicsDevice,384,160);tex.SetData(result);using var f=File.Create(args[2]);tex.SaveAsPng(f,384,160);
  Console.WriteLine("PASS: actual saving icon BGRA->RGBA pixel-exact; large texture uses RGBA4444; colours/alpha and odd-width partial uploads read back correctly; GL error=0.");Passed=true;Exit();
 }
 static int Delta(Color a,Color b)=>Math.Max(Math.Max(Math.Abs(a.R-b.R),Math.Abs(a.G-b.G)),Math.Max(Math.Abs(a.B-b.B),Math.Abs(a.A-b.A)));
 static void Check(Color actual,Color expected,int tolerance) {if(Delta(actual,expected)>tolerance)throw new Exception("Packed texture colour/alpha mismatch: "+actual+" expected "+expected);}
}
