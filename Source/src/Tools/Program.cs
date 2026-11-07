using Mono.Cecil;
using Mono.Cecil.Cil;
var resolver=new DefaultAssemblyResolver(); if(args[0]=="audit")resolver.AddSearchDirectory(args[2]); resolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!);
var a=AssemblyDefinition.ReadAssembly(args[1],new ReaderParameters{AssemblyResolver=resolver});
IEnumerable<TypeDefinition> All(IEnumerable<TypeDefinition> ts) { foreach(var t in ts) { yield return t; foreach(var child in All(t.NestedTypes))yield return child; } }
void MemoryHooks(AssemblyDefinition hook) {
 var helpers=a.MainModule.GetType("GSGE.Code.Helpers.CustomContentManager");
 var budget=hook.MainModule.GetType("BastionRGDS.MemoryBudget");
 foreach(var pair in new[]{("unloadPackage","PackageUnloaded"),("startNextPackage","BeforePackageLoad"),("forceLoadPackage","BeforePackageLoad")}) {
  var target=helpers.Methods.Single(m=>m.Name==pair.Item1);
  if(target.Body.Instructions.Any(i=>i.Operand is MethodReference mr && mr.DeclaringType.FullName==budget.FullName && mr.Name==pair.Item2))continue;
  var callback=budget.Methods.Single(m=>m.Name==pair.Item2);
  target.Body.GetILProcessor().InsertBefore(target.Body.Instructions[0],Instruction.Create(OpCodes.Call,a.MainModule.ImportReference(callback)));
 }
}
if(args[0]=="find") {
 foreach(var t in All(a.MainModule.Types))foreach(var m in t.Methods)if(t.FullName.Contains(args[2],StringComparison.OrdinalIgnoreCase)||m.Name.Contains(args[2],StringComparison.OrdinalIgnoreCase))Console.WriteLine(m);
} else if(args[0]=="patch13") {
 MemoryHooks(AssemblyDefinition.ReadAssembly(args[3]));a.Write(args[2]);Console.WriteLine("Added package-transition memory hooks to existing Alpha1.2 assembly");
} else if(args[0]=="inspect") {
 Console.WriteLine("ENTRY "+a.MainModule.EntryPoint);
 foreach(var r in a.MainModule.AssemblyReferences)Console.WriteLine("REF "+r);
 foreach(var m in a.MainModule.GetMemberReferences().Where(m=>m.DeclaringType.Scope.Name=="FMOD"))Console.WriteLine(m);
 foreach(var t in All(a.MainModule.Types))foreach(var m in t.Methods) {
  if(m.IsPInvokeImpl)Console.WriteLine("NATIVE "+m+" "+m.PInvokeInfo.Module.Name+":"+m.PInvokeInfo.EntryPoint);
  if(m.HasBody && (t.Name=="App" && new[]{".ctor","Main","Initialize"}.Contains(m.Name) || t.Name.Contains("Resolution"))) {
   Console.WriteLine("METHOD "+m); foreach(var i in m.Body.Instructions)Console.WriteLine(i);
  }
 }
} else if(args[0]=="audit") {
 resolver.AddSearchDirectory(args[2]);
 foreach(var m in a.MainModule.GetMemberReferences().Where(m=>m.DeclaringType.Scope.Name=="FMOD" || m.DeclaringType.Scope.Name=="FNA")) {
  try { if(m.Resolve()==null)Console.WriteLine("MISSING "+m); }catch(Exception ex) {Console.WriteLine("MISSING "+m+" : "+ex.Message);}
 }
} else if(args[0]=="patch") {
 // Hook per-frame hardware exit handling without changing simulation timing.
 var hook=AssemblyDefinition.ReadAssembly(args[3]);
 var method=hook.MainModule.GetType("BastionRGDS.Device").Methods.Single(m=>m.Name=="OnFrame");
 var app=a.MainModule.GetType("GSGE.App");
 // Use the game's supplied low-resolution atlas packages regardless of Wayland desktop size.
 var constructor=app.Methods.Single(m=>m.IsConstructor && !m.IsStatic);
 var packageThreshold=constructor.Body.Instructions.Single(i=>i.OpCode==OpCodes.Ldc_I4 && i.Operand is int value && value==900);
 packageThreshold.Operand=int.MaxValue;
 var update=app.Methods.Single(m=>m.Name=="Update" && m.Parameters.Count==1);
 var il=update.Body.GetILProcessor();var first=update.Body.Instructions[0];
 il.InsertBefore(first,il.Create(OpCodes.Ldarg_0));
 il.InsertBefore(first,il.Create(OpCodes.Call,a.MainModule.ImportReference(method)));
 foreach(var m in app.Methods.Where(m=>m.HasBody)) {
  var proc=m.Body.GetILProcessor();
  foreach(var call in m.Body.Instructions.Where(i=>(i.OpCode==OpCodes.Callvirt || i.OpCode==OpCodes.Call) && i.Operand is MethodReference mr && mr.DeclaringType.FullName=="Microsoft.Xna.Framework.GraphicsDeviceManager" && mr.Name=="set_IsFullScreen").ToArray()) {
   proc.InsertBefore(call,proc.Create(OpCodes.Pop));proc.InsertBefore(call,proc.Create(OpCodes.Ldc_I4_1));
  }
 }
 var drawHook=hook.MainModule.GetType("BastionRGDS.Device").Methods.Single(m=>m.Name=="AfterDraw");
 foreach(var draw in app.Methods.Where(m=>m.Name=="Draw" && m.HasBody)) {
  var proc=draw.Body.GetILProcessor();
  foreach(var ret in draw.Body.Instructions.Where(i=>i.OpCode==OpCodes.Ret).ToArray()) {proc.InsertBefore(ret,proc.Create(OpCodes.Ldarg_0));proc.InsertBefore(ret,proc.Create(OpCodes.Call,a.MainModule.ImportReference(drawHook)));}
 }
 // Force the physical backbuffer size through the existing command-line options.
 // All scene/content transformations are left to the original PC build for this first test.
 MemoryHooks(hook);
 a.MainModule.Attributes &= ~(ModuleAttributes.Required32Bit | ModuleAttributes.Preferred32Bit); a.Write(args[2]); Console.WriteLine("Patched frame hook only: "+update);
} else if(args[0]=="paths") { foreach(var t in All(a.MainModule.Types))foreach(var m in t.Methods.Where(m=>m.HasBody))if(m.Body.Instructions.Any(i=>(i.Operand is string st && st.Contains("720")) || (i.Operand is MethodReference mr && mr.DeclaringType.Name=="CustomContentManager" && mr.Name==".ctor"))) {Console.WriteLine("METHOD "+m);foreach(var i in m.Body.Instructions)Console.WriteLine(i);} } else if(args[0]=="textures") { foreach(var t in All(a.MainModule.Types).Where(t=>t.Name.Contains("Texture") || t.Name.Contains("Package") || t.Name.Contains("Config") || t.Name.Contains("Content"))) {Console.WriteLine("TYPE "+t.FullName);foreach(var f in t.Fields)Console.WriteLine("FIELD "+f);foreach(var m in t.Methods)Console.WriteLine("METHOD "+m); } } else if(args[0]=="async") { foreach(var t in All(a.MainModule.Types))foreach(var m in t.Methods.Where(m=>m.HasBody))foreach(var i in m.Body.Instructions)if(i.Operand is MethodReference r && (r.Name=="BeginInvoke" || r.Name=="EndInvoke"))Console.WriteLine(m+" -> "+r); } else if(args[0]=="dump") { foreach(var t in All(a.MainModule.Types).Where(t=>t.FullName==args[2]))foreach(var m in t.Methods.Where(m=>m.HasBody)) {Console.WriteLine("METHOD "+m);foreach(var i in m.Body.Instructions)Console.WriteLine(i);} } else throw new ArgumentException("Unknown command");
