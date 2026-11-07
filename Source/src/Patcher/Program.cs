// Standalone integration patcher, MIT. No game code or game binary is embedded.
using System.Security.Cryptography;
using Mono.Cecil;
using Mono.Cecil.Cil;
const string OriginalHash="e8d028fdca5069115202d45695cc44e4e9c39bd27179415c65cc59182db3d748";
try {
 if(args.Length!=3)throw new ArgumentException("Usage: Patcher ORIGINAL_OR_LOCAL_Bastion.exe OUTPUT.exe HOST.dll");
 string hash=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[0]))).ToLowerInvariant();
 using var a=AssemblyDefinition.ReadAssembly(args[0]);using var hook=AssemblyDefinition.ReadAssembly(args[2]);
 var app=a.MainModule.GetType("GSGE.App")??throw new InvalidDataException("Unsupported game assembly");
 var helper=a.MainModule.GetType("GSGE.Code.Helpers.CustomContentManager")??throw new InvalidDataException("Unsupported content loader");
 int Calls(MethodDefinition m,string type,string method)=>m.Body.Instructions.Count(i=>i.Operand is MethodReference mr&&mr.DeclaringType.FullName==type&&mr.Name==method);
 var update=app.Methods.Single(m=>m.Name=="Update"&&m.Parameters.Count==1);
 var draw=app.Methods.Single(m=>m.Name=="Draw"&&m.HasBody);
 bool prior=Calls(update,"BastionRGDS.Device","OnFrame")==1&&Calls(draw,"BastionRGDS.Device","AfterDraw")==1&&new[]{("unloadPackage","PackageUnloaded"),("startNextPackage","BeforePackageLoad"),("forceLoadPackage","BeforePackageLoad")}.All(p=>Calls(helper.Methods.Single(m=>m.Name==p.Item1),"BastionRGDS.MemoryBudget",p.Item2)==1)&&!(a.MainModule.Attributes.HasFlag(ModuleAttributes.Required32Bit));
 if(prior){File.Copy(args[0],args[1],true);Console.WriteLine("PREPARE: compatible local Alpha/Beta assembly retained; hooks were not duplicated.");return 0;}
 if(hash!=OriginalHash)throw new InvalidDataException("Unsupported Linux game build. Expected original SHA256 "+OriginalHash+"; received "+hash+". Your input file was not modified.");
 void Prefix(MethodDefinition method,MethodDefinition callback,bool instance){var il=method.Body.GetILProcessor();var first=method.Body.Instructions[0];if(instance)il.InsertBefore(first,il.Create(OpCodes.Ldarg_0));il.InsertBefore(first,il.Create(OpCodes.Call,a.MainModule.ImportReference(callback)));}
 var device=hook.MainModule.GetType("BastionRGDS.Device");var budget=hook.MainModule.GetType("BastionRGDS.MemoryBudget");
 Prefix(update,device.Methods.Single(m=>m.Name=="OnFrame"),true);
 var afterDraw=device.Methods.Single(m=>m.Name=="AfterDraw");
 foreach(var ret in draw.Body.Instructions.Where(i=>i.OpCode==OpCodes.Ret).ToArray()){
  var il=draw.Body.GetILProcessor();var first=il.Create(OpCodes.Ldarg_0);il.InsertBefore(ret,first);il.InsertBefore(ret,il.Create(OpCodes.Call,a.MainModule.ImportReference(afterDraw)));
  foreach(var i in draw.Body.Instructions){if(i.Operand==ret)i.Operand=first;else if(i.Operand is Instruction[] targets)for(int n=0;n<targets.Length;n++)if(targets[n]==ret)targets[n]=first;}
 }
 var ctor=app.Methods.Single(m=>m.IsConstructor&&!m.IsStatic);var threshold=ctor.Body.Instructions.Single(i=>i.OpCode==OpCodes.Ldc_I4&&i.Operand is int v&&v==900);threshold.Operand=int.MaxValue;
 foreach(var m in app.Methods.Where(m=>m.HasBody))foreach(var call in m.Body.Instructions.Where(i=>(i.OpCode==OpCodes.Callvirt||i.OpCode==OpCodes.Call)&&i.Operand is MethodReference mr&&mr.DeclaringType.FullName=="Microsoft.Xna.Framework.GraphicsDeviceManager"&&mr.Name=="set_IsFullScreen").ToArray()){
  var il=m.Body.GetILProcessor();il.InsertBefore(call,il.Create(OpCodes.Pop));il.InsertBefore(call,il.Create(OpCodes.Ldc_I4_1));
 }
 foreach(var pair in new[]{("unloadPackage","PackageUnloaded"),("startNextPackage","BeforePackageLoad"),("forceLoadPackage","BeforePackageLoad")})Prefix(helper.Methods.Single(m=>m.Name==pair.Item1),budget.Methods.Single(m=>m.Name==pair.Item2),false);
 a.MainModule.Attributes&=~(ModuleAttributes.Required32Bit|ModuleAttributes.Preferred32Bit);a.Write(args[1]);Console.WriteLine("PREPARE: supported Linux assembly patched locally for the standalone ARM64 runtime.");return 0;
}catch(Exception ex){Console.Error.WriteLine("PREPARATION ERROR: "+ex.Message);return 3;}
