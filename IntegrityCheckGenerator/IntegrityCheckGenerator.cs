using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#nullable enable

namespace IntegrityCheckGenerator
{
    [Generator]
    class IntegrityCheckGenerator : ISourceGenerator
    {
        private static readonly DiagnosticDescriptor ourGenerationFailed = new("ICG0000",
            "Generation failed", "{0}", "Generators", DiagnosticSeverity.Error, true); 
        
        public void Initialize(GeneratorInitializationContext context)
        {
        
        }

        public void Execute(GeneratorExecutionContext context)
        {
            string? modTypeName = null;
            string? modNamespace = null;

            foreach (var tree in context.Compilation.SyntaxTrees)
            foreach (var decl in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var baseTypeName = decl.BaseList?.Types.FirstOrDefault()?.Type.ToString();
                if (baseTypeName != "MelonMod") continue;

                if (decl.Modifiers.All(it => it.ValueText != "partial"))
                {
                    context.ReportDiagnostic(Diagnostic.Create(ourGenerationFailed, decl.GetLocation(), "Mod is not partial"));
                    continue;
                }

                if (modTypeName != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(ourGenerationFailed, decl.GetLocation(), "Too many mods in one project"));
                    continue;
                }

                modTypeName = decl.Identifier.ToString();
                var symbol = context.Compilation.GetSemanticModel(tree).GetDeclaredSymbol(decl);
                modNamespace = symbol!.ContainingNamespace.ToDisplayString();
                // hasSceneLoadedDerivative = symbol;
            }

            if (modTypeName == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(ourGenerationFailed, null, "Too many mods in one project"));
                return;
            }

            var generatedCode = new StringBuilder();

            generatedCode.AppendLine("using HarmonyLib;");
            generatedCode.AppendLine("using System;");
            generatedCode.AppendLine("using System.Collections;");
            generatedCode.AppendLine("using System.IO;");
            generatedCode.AppendLine("using System.Linq;");
            generatedCode.AppendLine("using System.Reflection;");
            generatedCode.AppendLine("using System.Runtime.InteropServices;");
            generatedCode.AppendLine("using MelonLoader;");
            generatedCode.AppendLine("using UnityEngine;");
            
            generatedCode.AppendLine($"namespace {modNamespace} {{");
            generatedCode.AppendLine($"partial class {modTypeName} {{");
        
            generatedCode.AppendLine("internal static bool CheckWasSuccessful = true;");
            generatedCode.AppendLine("internal static bool MustStayFalse = false;");
            generatedCode.AppendLine("internal static bool MustStayTrue = true;");
            generatedCode.AppendLine("internal static bool RanCheck3 = false;");
            generatedCode.AppendLine("private static readonly Func<VRCUiManager> ourGetUiManager;");

            generatedCode.AppendLine($"static {modTypeName}() {{");
            generatedCode.AppendLine("ourGetUiManager = (Func<VRCUiManager>) Delegate.CreateDelegate(typeof(Func<VRCUiManager>), typeof(VRCUiManager)");
            generatedCode.AppendLine("    .GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)");
            generatedCode.AppendLine("    .First(it => it.PropertyType == typeof(VRCUiManager)).GetMethod);");
            generatedCode.AppendLine("}");
        
            generatedCode.AppendLine("partial void OnSceneWasLoaded2(int buildIndex, string sceneName);");
            generatedCode.AppendLine("public override void OnSceneWasLoaded(int buildIndex, string sceneName)");
            generatedCode.AppendLine("{");
            generatedCode.AppendLine("    if (buildIndex != -1 || RanCheck3) return;");
            generatedCode.AppendLine("    RanCheck3 = true;");
            generatedCode.AppendLine("    OnSceneWasLoaded2(buildIndex, sceneName);");
            generatedCode.AppendLine("}");
        
            generatedCode.AppendLine("internal static VRCUiManager GetUiManager() => ourGetUiManager();");
        
            generatedCode.AppendLine("private static void DoAfterUiManagerInit(Action code) {");
            generatedCode.AppendLine("    MelonCoroutines.Start(OnUiManagerInitCoro(code));");
            generatedCode.AppendLine("}");

            generatedCode.AppendLine("private static IEnumerator OnUiManagerInitCoro(Action code) {");
            generatedCode.AppendLine("    while (GetUiManager() == null)");
            generatedCode.AppendLine("        yield return null;");
            generatedCode.AppendLine("    code();");
            generatedCode.AppendLine("}");
            
            generatedCode.AppendLine("internal static void CheckA() {}");
            generatedCode.AppendLine("internal static void CheckB() {}");
            generatedCode.AppendLine("internal static void CheckC() {}");

            generatedCode.AppendLine("private static bool ReturnFalse() => false;");
            generatedCode.AppendLine("private static void PatchTest() => throw new BadImageFormatException();");
            generatedCode.AppendLine("private static void PatchTast() => throw new BadImageFormatException();");
            
            generatedCode.AppendLine("internal static void CheckDummyThree() {}");

            generatedCode.AppendLine("}");
            generatedCode.AppendLine("}");

            context.AddSource($"{modTypeName}.Generated.cs", generatedCode.ToString());
        }
    }
}
