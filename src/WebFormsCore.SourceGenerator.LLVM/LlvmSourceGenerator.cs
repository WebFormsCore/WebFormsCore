using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Scriban;

namespace WebFormsCore.SourceGenerator.LLVM;

[Generator]
public class LlvmSourceGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var typeDeclaration = context.SyntaxProvider
			.CreateSyntaxProvider(
				predicate: Predicate,
				transform: static (ctx, _) => (TypeDeclarationSyntax)ctx.Node);

		context.RegisterSourceOutput(typeDeclaration.Combine(context.AnalyzerConfigOptionsProvider).Combine(context.CompilationProvider),
			static (spc, source) => Execute(source, spc));
	}

	private static bool Predicate(SyntaxNode s, CancellationToken token)
	{
		return s is TypeDeclarationSyntax { Identifier.Text: "Startup" };
	}

	private static void Execute(((TypeDeclarationSyntax Left, AnalyzerConfigOptionsProvider Right) Left, Compilation Right) source, SourceProductionContext spc)
	{
		const string templateFile = "Templates/llvm.scriban";
		var template = Template.Parse(EmbeddedResource.GetContent(templateFile), templateFile);

		var (type, analyzer) = source.Left;
		var compilation = source.Right;

		if (!analyzer.GlobalOptions.TryGetValue("build_property.RootNamespace", out var ns))
		{
			ns = compilation.AssemblyName;
		}

		const string configureServices = "ConfigureServices";
		const string configure = "Configure";

		var hasConfigureServices = false;
		List<string>? configureParameters = null;

		var model = compilation.GetSemanticModel(type.SyntaxTree);

		foreach (var method in type.Members.OfType<MethodDeclarationSyntax>())
		{
			if (method.Identifier.Text == configureServices)
			{
				hasConfigureServices = true;
			}
			else if (method.Identifier.Text == configure)
			{
				configureParameters = method.ParameterList.Parameters
					.Where(i => i.Type != null)
					.Select(i => model.GetSymbolInfo(i.Type!).Symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
					.Where(i => i != null)
					.ToList()!;
			}
		}

		spc.AddSource("Startup", template.Render(new
		{
			Type = model.GetDeclaredSymbol(type)?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			Namespace = ns,
			ConfigureServices = hasConfigureServices,
			ConfigureParameters = configureParameters
		}, member => member.Name));
	}
}
