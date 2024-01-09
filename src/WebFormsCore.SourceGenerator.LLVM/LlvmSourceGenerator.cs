using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
				transform: static (ctx, _) => Execute(ctx));

		var currentNamespace = context.AnalyzerConfigOptionsProvider
			.Select((options, _) => options.GlobalOptions.TryGetValue("build_property.RootNamespace", out var ns) ? ns : null);

		var result = typeDeclaration.Combine(currentNamespace)
			.Select((i, _) => i.Right is not null ? i.Left with { Namespace = i.Right } : i.Left);

		context.RegisterSourceOutput(result,
			static (spc, source) =>
			{
				const string templateFile = "Templates/llvm.scriban";
				var template = Template.Parse(EmbeddedResource.GetContent(templateFile), templateFile);
				spc.AddSource("Startup", template.Render(source, member => member.Name));
			});
	}

	private static bool Predicate(SyntaxNode s, CancellationToken token)
	{
		return s is TypeDeclarationSyntax { Identifier.Text: "Startup" };
	}

	private static SourceInformation Execute(GeneratorSyntaxContext ctx)
	{
		var type = (TypeDeclarationSyntax)ctx.Node;
		var compilation = ctx.SemanticModel.Compilation;

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

		return new SourceInformation(
			Type: model.GetDeclaredSymbol(type)?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)!,
			Namespace: compilation.AssemblyName,
			ConfigureServices: hasConfigureServices,
			ConfigureParameters: configureParameters
		);
	}

	public record SourceInformation(string Type, string? Namespace, bool ConfigureServices, List<string>? ConfigureParameters);
}
