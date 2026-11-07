// RGDS+ private port integration, MIT. Scheduling diagnostics preserve the fixed physics step.
using System.Diagnostics;
using Microsoft.Xna.Framework;
namespace BastionRGDS;
public static class BattlePerformance {
 static bool announced;static long updates,draws,updateTicks,drawTicks,discarded;
 public static void Configure(Game game){if(announced)return;announced=true;Console.WriteLine($"SCHEDULER: fixed={game.IsFixedTimeStep}; physics step={game.TargetElapsedTime.TotalMilliseconds:F3}ms; max catch-up steps/draw={game.MaxUpdateStepsPerDrawEXT} (0=original)");}
 public static void Log(Game game){long u=game.TotalUpdateCallsEXT,d=game.TotalDrawCallsEXT,ut=game.TotalUpdateStopwatchTicksEXT,dt=game.TotalDrawStopwatchTicksEXT,drop=game.DiscardedCatchUpTicksEXT;
  double scale=1000.0/Stopwatch.Frequency;
  Console.WriteLine($"FRAME COST: update avg={(u>updates?(ut-updateTicks)*scale/(u-updates):0):F2}ms; draw+present avg={(d>draws?(dt-drawTicks)*scale/(d-draws):0):F2}ms; max update batch={game.MaxUpdateBatchEXT}; discarded catch-up={(drop-discarded)/(double)TimeSpan.TicksPerSecond:F3}s; GC gen0={GC.CollectionCount(0)} gen1={GC.CollectionCount(1)} gen2={GC.CollectionCount(2)}");updates=u;draws=d;updateTicks=ut;drawTicks=dt;discarded=drop;
 }
}
