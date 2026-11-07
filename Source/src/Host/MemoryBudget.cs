// RGDS+ private port integration, MIT. Collect only between content package loads.
using System.Runtime.InteropServices;
namespace BastionRGDS;
public static class MemoryBudget {
 static int pending,cycles;
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Finish();
 [DllImport("libSDL2-2.0.so.0")] static extern IntPtr SDL_GL_GetCurrentContext();
 [DllImport("libSDL2-2.0.so.0")] static extern IntPtr SDL_GL_GetProcAddress(string name);
 [DllImport("libc.so.6")] static extern int malloc_trim(nuint pad);
 public static void PackageUnloaded() => Interlocked.Exchange(ref pending,1);
 public static void BeforePackageLoad() {
  if(Interlocked.Exchange(ref pending,0)==0)return;
  try {
   // Ensure deleted graphics resources have finished before another package allocates.
   if(SDL_GL_GetCurrentContext()!=IntPtr.Zero) {
    var ptr=SDL_GL_GetProcAddress("glFinish");
    if(ptr!=IntPtr.Zero)Marshal.GetDelegateForFunctionPointer<Finish>(ptr)();
   }
   GC.Collect(GC.MaxGeneration,GCCollectionMode.Forced,true,false);
   // Graphics finalizers can queue work to this thread: never wait for them here.
   malloc_trim(0);
   Console.WriteLine("MEMORY: reclaimed unused buffers before package load; cycle="+(++cycles));
  }catch(Exception ex) when(ex is DllNotFoundException || ex is EntryPointNotFoundException) { Console.WriteLine("MEMORY: native trim unavailable: "+ex.Message); }
 }
}
