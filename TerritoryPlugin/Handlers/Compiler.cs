using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Sandbox.ModAPI;
using Torch.API.Managers;
using Torch.Commands;
using Torch.Managers.PatchManager;

namespace CrunchGroup.Handlers
{
    public static class Compiler
    {
        public static bool Compile(string folder)
        {
            bool success = CompileFromFile(folder);

            return success;
        }

        public static MetadataReference[] GetRequiredRefernces()
        {
            List<MetadataReference> metadataReferenceList = new List<MetadataReference>();
            foreach (Assembly assembly in ((IEnumerable<Assembly>)AppDomain.CurrentDomain.GetAssemblies()).Where<Assembly>((Func<Assembly, bool>)(a => !a.IsDynamic)))
            {
                if (!assembly.IsDynamic && assembly.Location != null & string.Empty != assembly.Location)
                {
                    if (!IsAssemblyExcluded(assembly))
                    {
                        metadataReferenceList.Add((MetadataReference)MetadataReference.CreateFromFile(assembly.Location));
                    }
                }
            }

            foreach (var filePath in Directory.GetFiles($"{Core.basePath}/{Core.PluginName}/").Where(x => x.Contains(".dll")))
            {
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    metadataReferenceList.Add(MetadataReference.CreateFromStream(fileStream));
                }
            }

            foreach (var filePath in Directory.GetFiles(Core.path).Where(x => x.Contains(".dll")))
            {
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    metadataReferenceList.Add(MetadataReference.CreateFromStream(fileStream));
                }
            }
            return metadataReferenceList.ToArray();
        }
        private static bool CompileFromFile(string folder)
        {
            var patches = Core.Session.Managers.GetManager<PatchManager>();
            var commands = Core.Session.Managers.GetManager<CommandManager>();
            List<SyntaxTree> trees = new List<SyntaxTree>();

            try
            {
                foreach (var filePath in Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
                             .Where(x => x.EndsWith(".cs")))
                {
                    using (FileStream fileStream =
                           new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (StreamReader streamReader = new StreamReader(fileStream))
                        {
                            string text = streamReader.ReadToEnd();
                            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(text);
                            trees.Add(syntaxTree);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Core.Log.Error($"Compiler file error {e}");
            }

            var compilation = CSharpCompilation.Create("MyAssembly")
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(GetRequiredRefernces()) // Add necessary references
                .AddSyntaxTrees(trees);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                var result = compilation.Emit(memoryStream);

                if (result.Success)
                {
                    Assembly assembly = Assembly.Load(memoryStream.ToArray());
                    Core.Log.Error("Compilation successful!");
                    Core.myAssemblies.Add(assembly);

                    try
                    {
                        foreach (var type in assembly.GetTypes())
                        {
                            MethodInfo method = type.GetMethod("Patch", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                            if (method == null)
                            {
                                continue;
                            }
                            ParameterInfo[] ps = method.GetParameters();
                            if (ps.Length != 1 || ps[0].IsOut || ps[0].IsOptional || ps[0].ParameterType.IsByRef ||
                                ps[0].ParameterType != typeof(PatchContext) || method.ReturnType != typeof(void))
                            {
                                continue;
                            }
                            var context = patches.AcquireContext();
                            method.Invoke(null, new object[] { context });
                        }
                        patches.Commit();
                        foreach (var obj in assembly.GetTypes())
                        {
                            commands.RegisterCommandModule(obj);
                        }
                    }
                    catch (Exception e)
                    {
                        Core.Log.Error($"{e}");
                        throw;
                    }
                }
                else
                {
                    Console.WriteLine("Compilation failed:");
                    foreach (var diagnostic in result.Diagnostics)
                    {
                        Core.Log.Error(diagnostic);
                    }
                }
            }
            return true;
        }
        private static bool IsAssemblyExcluded(Assembly assembly)
        {
            // Replace "protobuf-net" with the actual name of the assembly you want to exclude
            return assembly.GetName().Name == "protobuf-net";
        }
    }


}
