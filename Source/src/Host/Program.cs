using System.Reflection;
using System.Runtime.Loader;
using BastionRGDS;
internal static class Program {
 [STAThread] static int Main(string[] args) {
  Environment.SetEnvironmentVariable("FNA_PLATFORM_BACKEND","SDL2");
  AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization",true);
  AssemblyLoadContext.Default.Resolving+=(context,name)=> {
   var p=Path.Combine(AppContext.BaseDirectory,name.Name+".dll");
   return File.Exists(p)?context.LoadFromAssemblyPath(p):null;
  };
  if(args.Contains("--ensure-controls") || args.Contains("--calibrate-controls"))return ControlCalibration.Run(args.Contains("--calibrate-controls"));
  try {
   Console.WriteLine("Bastion RGDS+ Beta 1 (1.0.0-beta.1)");
   Console.WriteLine("Runtime "+System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription+" / "+System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture);
   FMOD.RGDSVolume.Initialize();
   Device.ConfigureControllers();
   HardwareTriggers.Initialize();
   var a=Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory,"Bastion.exe"));
   if(args.Contains("--check-assembly")) { var types=a.GetTypes(); Console.WriteLine("ASSEMBLY CHECK: "+types.Length+" types loaded"); return 0; }
   a.EntryPoint.Invoke(null,new object[]{args});return 0;
  } catch(Exception ex) {
   if(ex is TargetInvocationException && ex.InnerException!=null)ex=ex.InnerException;
   Console.Error.WriteLine("PORT ERROR: "+ex);
   if(ex is ReflectionTypeLoadException rt)foreach(var error in rt.LoaderExceptions)Console.Error.WriteLine(error);
   return 1;
  } finally { HardwareTriggers.Shutdown(); try { FMOD.Framework.terminate(); } finally { FMOD.RGDSVolume.Shutdown(); } }
 }
}
