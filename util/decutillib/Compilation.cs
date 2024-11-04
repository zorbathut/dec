using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace DecUtilLib
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public static class Compilation
    {
        private static Dictionary<Assembly, MemoryStream> AssemblyStreams = new Dictionary<Assembly, MemoryStream>();

        public static Assembly Compile(string src, Assembly[] assemblies)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(src);
            string assemblyName = Path.GetRandomFileName() + ".DynComp.dll";
            var refPaths = new[] {
                    typeof(object).GetTypeInfo().Assembly.Location,
                    typeof(Dec.Dec).GetTypeInfo().Assembly.Location,
                    typeof(NUnit.Framework.Assert).GetTypeInfo().Assembly.Location,
                    typeof(Compilation).GetType().GetTypeInfo().Assembly.Location,
                    Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "netstandard.dll"),
                    Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Runtime.dll"),
                    Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Collections.dll"),
                };

            var references = refPaths.Select(r => MetadataReference.CreateFromFile(r)).Concat(assemblies.Select(asm =>
            {
                if (AssemblyStreams.ContainsKey(asm))
                {
                    var stream = AssemblyStreams[asm];
                    stream.Seek(0, SeekOrigin.Begin);
                    return MetadataReference.CreateFromStream(stream);
                }
                else
                {
                    return MetadataReference.CreateFromFile(asm.Location);
                }
            })).ToArray();


            var compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary));

            var ms = new MemoryStream();
            var result = compilation.Emit(ms);
            if (!result.Success)
            {
                Assert.IsTrue(false, string.Join("\n", result.Diagnostics.Take(10).Select(err => err.ToString())));
            }

            ms.Seek(0, SeekOrigin.Begin);

            var assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
            AssemblyStreams[assembly] = ms;
            return assembly;
        }
    }
}
