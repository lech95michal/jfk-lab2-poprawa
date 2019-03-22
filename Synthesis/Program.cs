namespace Synthesis
{
    using System;
    using System.IO;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;

    public static class Program
    {
        private static readonly string AssemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);

        public static void Main()
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText("Class.cs"));

            var newRoot = new Rewriter().Visit(tree.GetRoot());
            File.WriteAllText(@"..\..\Class.altered.cs", newRoot.GetText().ToString());

            var mscorlib = MetadataReference.CreateFromFile(Path.Combine(AssemblyPath, "mscorlib.dll"));
            var system = MetadataReference.CreateFromFile(Path.Combine(AssemblyPath, "System.dll"));
            var systemCore = MetadataReference.CreateFromFile(Path.Combine(AssemblyPath, "System.Core.dll"));

            var compilation = CSharpCompilation.Create(
                "Altered",
                syntaxTrees: new[] { newRoot.SyntaxTree },
                references: new[] { mscorlib, system, systemCore },
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

            Console.ReadKey();
        }
    }
}
