namespace DefUtilLib
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Loader;

    public static class Compilation
    {
        public static Assembly Compile(string src, Assembly[] assemblies)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(src);
            string assemblyName = Path.GetRandomFileName();
            var refPaths = new[] {
                    typeof(object).GetTypeInfo().Assembly.Location,
                    typeof(Def.Def).GetTypeInfo().Assembly.Location,
                    typeof(NUnit.Framework.Assert).GetTypeInfo().Assembly.Location,
                    typeof(Compilation).GetType().GetTypeInfo().Assembly.Location,
                    Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "netstandard.dll"),
                    Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Runtime.dll"),
                    Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Collections.dll"),
                }.Concat(assemblies.Select(asm => asm.Location)).ToArray();

            var references = refPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray();

            var compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary));

            var ms = new MemoryStream();
            var result = compilation.Emit(ms);
            if (!result.Success)
            {
                Assert.IsTrue(false, string.Join("\n", result.Diagnostics.Select(err => err.ToString())));
            }

            ms.Seek(0, SeekOrigin.Begin);

            var assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
            return assembly;
        }

        public static object CompileAndRun(string code, Assembly[] assemblies, string functionName, params object[] param)
        {
            string source = @"
                    using NUnit.Framework;

                    public static class TestClass
                    {
                        " + code + @"
                    }";

            var assembly = Compile(source, assemblies);
            var t = assembly.GetType("TestClass");
            var m = t.GetMethod(functionName);

            return m.Invoke(null, param);
        }
    }
}
