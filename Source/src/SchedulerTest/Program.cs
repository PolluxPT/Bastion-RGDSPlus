using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;
using System.Text.Json;
using var game=new Probe();game.Run();var stats=new{limit=game.MaxUpdateStepsPerDrawEXT,updates=game.TotalUpdateCallsEXT,draws=game.TotalDrawCallsEXT,maxBatch=game.MaxUpdateBatchEXT,maxDrawGapMs=game.MaxGap,discardedSeconds=game.DiscardedCatchUpTicksEXT/(double)TimeSpan.TicksPerSecond,wallSeconds=game.Clock.Elapsed.TotalSeconds};Console.WriteLine("RESULT "+JsonSerializer.Serialize(stats));if(game.MaxUpdateStepsPerDrawEXT==2 && (game.MaxUpdateBatchEXT>2||game.MaxGap>300))throw new Exception("Bounded catch-up failed");if(game.MaxUpdateStepsPerDrawEXT==0&&game.MaxUpdateBatchEXT<10)throw new Exception("Baseline did not reproduce catch-up amplification");
class Probe:Game {
 readonly GraphicsDeviceManager manager;public Stopwatch Clock=new();public double MaxGap;double lastDraw;
 public Probe(){manager=new GraphicsDeviceManager(this){PreferredBackBufferWidth=64,PreferredBackBufferHeight=64,SynchronizeWithVerticalRetrace=false};InactiveSleepTime=TimeSpan.Zero;}
 protected override void LoadContent(){Clock.Start();Console.WriteLine("Catch-up mode "+MaxUpdateStepsPerDrawEXT);}
 protected override void Update(GameTime t){if(t.ElapsedGameTime!=TargetElapsedTime)throw new Exception("Physics step changed");if(Clock.Elapsed.TotalSeconds>6){Exit();return;}if(Clock.Elapsed.TotalSeconds>1)Thread.Sleep(25);}
 protected override void Draw(GameTime t){double now=Clock.Elapsed.TotalMilliseconds;if(lastDraw>0)MaxGap=Math.Max(MaxGap,now-lastDraw);lastDraw=now;GraphicsDevice.Clear(Color.Black);Thread.Sleep(5);}
}
