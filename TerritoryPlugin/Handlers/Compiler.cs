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

namespace CrunchGroup.Handlers
{
    public static class Compiler
    {
        public static bool Compile(string file)
        {
            bool success = CompileFromFile(file);

            return success;
        }

        public static MetadataReference[] GetRequiredRefernces()
        {
            List<MetadataReference> metadataReferenceList = new List<MetadataReference>();
            foreach (Assembly assembly in ((IEnumerable<Assembly>)AppDomain.CurrentDomain.GetAssemblies()).Where<Assembly>((Func<Assembly, bool>)(a => !a.IsDynamic)))
            {
                if (!assembly.IsDynamic && assembly.Location != null & string.Empty != assembly.Location)
                    metadataReferenceList.Add((MetadataReference)MetadataReference.CreateFromFile(assembly.Location));
            }

            ///  metadataReferenceList.Add(MetadataReference.CreateFromFile(@$"{Core.path}\CrunchEconV3.dll"));
            return metadataReferenceList.ToArray();
        }
        private static bool CompileFromFile(string file)
        {
            var text = File.ReadAllText(file);
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(text);

            var compilation = CSharpCompilation.Create("MyAssembly")
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(GetRequiredRefernces()) // Add necessary references
                .AddSyntaxTrees(syntaxTree);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                var result = compilation.Emit(memoryStream);

                if (result.Success)
                {
                    Assembly assembly = Assembly.Load(memoryStream.ToArray());
                    Core.Log.Error("Compilation successful!");
                    Core.myAssemblies.Add(assembly);
                    // Use the compiled assembly as needed
                }
                else
                {
                    Core.Log.Error(file);
                    Console.WriteLine("Compilation failed:");
                    foreach (var diagnostic in result.Diagnostics)
                    {
                        Core.Log.Error(diagnostic);
                    }
                }
            }
            return true;
        }
    }
}
