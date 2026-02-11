using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;
using WebFormsCore.SourceGenerator.Models;

namespace WebFormsCore.SourceGenerator;

[Generator]
public class ControlEventsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var languageVersion = context.CompilationProvider
            .Select(static (c, _) => c is CSharpCompilation cs ? (int)cs.LanguageVersion : 0);

        var controlTypes = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is TypeDeclarationSyntax { BaseList.Types.Count: > 0 },
                transform: static (ctx, token) => GetControlTypeInfo(ctx, token))
            .Where(static info => info.Events.Length > 0);

        var compilationAndControls = controlTypes
            .Collect()
            .Combine(languageVersion);

        context.RegisterSourceOutput(compilationAndControls, static (spc, source) => Execute(spc, source.Left, source.Right));
    }

    private static ControlTypeModel GetControlTypeInfo(GeneratorSyntaxContext context, CancellationToken token)
    {
        if (context.Node is not TypeDeclarationSyntax typeDeclaration)
        {
            return default;
        }

        if (context.SemanticModel.GetDeclaredSymbol(typeDeclaration, token) is not INamedTypeSymbol typeSymbol)
        {
            return default;
        }

        if (typeSymbol.TypeKind != TypeKind.Class || typeSymbol.DeclaredAccessibility == Accessibility.Private)
        {
            return default;
        }

        if (typeSymbol.IsAbstract)
        {
            return default;
        }

        if (!IsControl(typeSymbol))
        {
            return default;
        }

        var compilation = context.SemanticModel.Compilation;
        var asyncEventHandler = compilation.GetTypeByMetadataName("WebFormsCore.AsyncEventHandler");
        var asyncEventHandlerGeneric = compilation.GetTypeByMetadataName("WebFormsCore.AsyncEventHandler`2");

        var events = ImmutableArray.CreateBuilder<ControlEventInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IEventSymbol evt)
            {
                continue;
            }

            if (evt.IsStatic)
            {
                continue;
            }

            if (evt.ExplicitInterfaceImplementations.Length > 0)
            {
                continue;
            }

            if (evt.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
            {
                continue;
            }

            if (evt.Type is not INamedTypeSymbol eventType)
            {
                continue;
            }

            var accessibility = GetEffectiveAccessibility(typeSymbol, evt, eventType);
            var eventTypeDisplay = eventType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            EventHandlerKind kind;
            string? senderDisplay = null;
            string? argsDisplay = null;

            if (asyncEventHandler is not null && SymbolEqualityComparer.Default.Equals(eventType, asyncEventHandler))
            {
                kind = EventHandlerKind.Async;
            }
            else if (asyncEventHandlerGeneric is not null && SymbolEqualityComparer.Default.Equals(eventType.OriginalDefinition, asyncEventHandlerGeneric))
            {
                if (eventType.TypeArguments.Length != 2)
                {
                    continue;
                }

                kind = EventHandlerKind.AsyncGeneric;
                senderDisplay = eventType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                argsDisplay = eventType.TypeArguments[1].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
            else
            {
                kind = EventHandlerKind.Default;
            }

            events.Add(new ControlEventInfo(
                EventName: evt.Name,
                HandlerKind: kind,
                Accessibility: accessibility,
                EventTypeDisplay: eventTypeDisplay,
                GenericSenderTypeDisplay: senderDisplay,
                GenericArgsTypeDisplay: argsDisplay
            ));
        }

        if (events.Count == 0)
        {
            return default;
        }

        // Extract type parameter info
        var typeParams = ImmutableArray.CreateBuilder<TypeParameterModel>();

        foreach (var param in typeSymbol.TypeParameters)
        {
            var constraints = ImmutableArray.CreateBuilder<string>();

            if (param.HasUnmanagedTypeConstraint)
            {
                constraints.Add("unmanaged");
            }
            else if (param.HasReferenceTypeConstraint)
            {
                constraints.Add("class");
            }
            else if (param.HasValueTypeConstraint)
            {
                constraints.Add("struct");
            }

            if (param.HasNotNullConstraint)
            {
                constraints.Add("notnull");
            }

            foreach (var constraintType in param.ConstraintTypes)
            {
                constraints.Add(constraintType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }

            if (param.HasConstructorConstraint)
            {
                constraints.Add("new()");
            }

            typeParams.Add(new TypeParameterModel(
                Name: param.Name,
                Constraints: constraints.ToImmutable()
            ));
        }

        return new ControlTypeModel(
            FullyQualifiedName: typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            IsPublic: typeSymbol.DeclaredAccessibility == Accessibility.Public,
            TypeParameters: typeParams.ToImmutable(),
            Events: events.ToImmutable()
        );
    }

    private static bool IsControl(INamedTypeSymbol typeSymbol)
    {
        var current = typeSymbol;

        while (current is not null)
        {
            if (current.Name == "Control" && current.ContainingNamespace.ToString() == "WebFormsCore.UI")
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<ControlTypeModel> controls, int languageVersion)
    {
        if (languageVersion is > 0 and < 1400)
        {
            return;
        }

        var distinctControls = controls
            .GroupBy(static c => c.FullyQualifiedName)
            .Select(static g => g.First())
            .OrderBy(static c => c.FullyQualifiedName)
            .ToArray();

        if (distinctControls.Length == 0)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("namespace WebFormsCore.UI;");
        sb.AppendLine();

        foreach (var control in distinctControls)
        {
            var eventModels = CreateEventModels(control);

            if (eventModels.Length == 0)
            {
                continue;
            }

            var className = GetExtensionClassName(control);
            var classAccessibility = control.IsPublic ? "public" : "internal";
            sb.AppendLine($"{classAccessibility} static partial class {className}");
            sb.AppendLine("{");

            var extensionSignature = GetExtensionSignature(control);
            sb.AppendLine($"    {extensionSignature.Signature}");
            sb.Append(extensionSignature.Constraints);

            sb.AppendLine("    {");

            foreach (var evt in eventModels)
            {
                sb.AppendLine($"        {evt.Accessibility} {evt.PropertyType} {evt.Name}");
                sb.AppendLine("        {");
                sb.AppendLine($"            set => control.{evt.EventName} += {evt.Assignment};");
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        context.AddSource("ControlEvents.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private const string ReceiverTypeParameterName = "T";

    private static string GetExtensionClassName(ControlTypeModel control)
    {
        // Extract the simple type name from the fully qualified name
        // e.g., "global::WebFormsCore.UI.WebControls.Choices" -> "ChoicesExtensions"
        var fullName = control.FullyQualifiedName;
        var lastDot = fullName.LastIndexOf('.');
        var simpleName = lastDot >= 0 ? fullName.Substring(lastDot + 1) : fullName;

        // Handle generic types: "Repeater<T>" -> "RepeaterExtensions"
        var genericIndex = simpleName.IndexOf('<');
        if (genericIndex >= 0)
        {
            simpleName = simpleName.Substring(0, genericIndex);
        }

        return $"{simpleName}Extensions";
    }

    private static EventModel[] CreateEventModels(ControlTypeModel control)
    {
        var models = new List<EventModel>();
        var senderType = control.TypeParameters.Length == 0 ? ReceiverTypeParameterName : control.FullyQualifiedName;

        foreach (var evt in control.Events)
        {
            var asyncPropertyName = $"On{evt.EventName}Async";
            var syncPropertyName = $"On{evt.EventName}";

            switch (evt.HandlerKind)
            {
                case EventHandlerKind.Async:
                {
                    var funcType = $"global::System.Func<{senderType}, global::System.EventArgs, global::System.Threading.Tasks.Task>";
                    var assignment = $"async (sender, args) => await value(({senderType})sender, args)";
                    models.Add(new EventModel(asyncPropertyName, evt.EventName, evt.Accessibility, funcType, assignment));

                    var actionType = $"global::System.Action<{senderType}, global::System.EventArgs>";
                    var syncAssignment = $"(sender, args) => {{ value(({senderType})sender, args); return global::System.Threading.Tasks.Task.CompletedTask; }}";
                    models.Add(new EventModel(syncPropertyName, evt.EventName, evt.Accessibility, actionType, syncAssignment));
                    break;
                }

                case EventHandlerKind.AsyncGeneric:
                {
                    var argsType = evt.GenericArgsTypeDisplay!;
                    var funcType = $"global::System.Func<{senderType}, {argsType}, global::System.Threading.Tasks.Task>";
                    var assignment = $"async (sender, args) => await value(({senderType})sender, args)";
                    models.Add(new EventModel(asyncPropertyName, evt.EventName, evt.Accessibility, funcType, assignment));

                    var actionType = $"global::System.Action<{senderType}, {argsType}>";
                    var syncAssignment = $"(sender, args) => {{ value(({senderType})sender, args); return global::System.Threading.Tasks.Task.CompletedTask; }}";
                    models.Add(new EventModel(syncPropertyName, evt.EventName, evt.Accessibility, actionType, syncAssignment));
                    break;
                }

                default:
                    models.Add(new EventModel(syncPropertyName, evt.EventName, evt.Accessibility, evt.EventTypeDisplay, "value"));
                    break;
            }
        }

        return models.ToArray();
    }

    private static ExtensionSignature GetExtensionSignature(ControlTypeModel control)
    {
        if (control.TypeParameters.Length == 0)
        {
            // Non-generic control: use generic receiver for typed callbacks
            var signature = $"extension<{ReceiverTypeParameterName}>({ReceiverTypeParameterName} control)";
            var constraints = $"        where {ReceiverTypeParameterName} : {control.FullyQualifiedName}{Environment.NewLine}";
            return new ExtensionSignature(signature, constraints);
        }
        else
        {
            // Generic control: use concrete receiver with type parameters
            var typeParameters = GetTypeParameters(control);
            var constraints = GetTypeConstraints(control);
            var signature = $"extension{typeParameters}({control.FullyQualifiedName} control)";
            return new ExtensionSignature(signature, constraints.Length == 0 ? string.Empty : $"{constraints}{Environment.NewLine}");
        }
    }

    private static string GetTypeParameters(ControlTypeModel control)
    {
        if (control.TypeParameters.Length == 0)
        {
            return string.Empty;
        }

        var parameters = string.Join(", ", control.TypeParameters.AsImmutableArray().Select(static p => p.Name));
        return $"<{parameters}>";
    }

    private static string GetTypeConstraints(ControlTypeModel control)
    {
        if (control.TypeParameters.Length == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        foreach (var parameter in control.TypeParameters)
        {
            if (parameter.Constraints.Length == 0)
            {
                continue;
            }

            sb.AppendLine($"        where {parameter.Name} : {string.Join(", ", parameter.Constraints)}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string GetEffectiveAccessibility(INamedTypeSymbol controlType, IEventSymbol evt, INamedTypeSymbol eventType)
    {
        if (controlType.DeclaredAccessibility != Accessibility.Public)
        {
            return "internal";
        }

        if (evt.DeclaredAccessibility != Accessibility.Public)
        {
            return "internal";
        }

        if (!IsTypePublic(eventType))
        {
            return "internal";
        }

        return "public";
    }

    private static bool IsTypePublic(ITypeSymbol type)
    {
        switch (type)
        {
            case ITypeParameterSymbol:
                return true;
            case IArrayTypeSymbol array:
                return IsTypePublic(array.ElementType);
            case INamedTypeSymbol named:
                if (named.DeclaredAccessibility != Accessibility.Public)
                {
                    return false;
                }

                if (named.ContainingType is not null && named.ContainingType.DeclaredAccessibility != Accessibility.Public)
                {
                    return false;
                }

                foreach (var argument in named.TypeArguments)
                {
                    if (!IsTypePublic(argument))
                    {
                        return false;
                    }
                }

                return true;
            default:
                return true;
        }
    }

    // Pipeline data types — all use structural equality for proper incremental caching
    private enum EventHandlerKind
    {
        Default,
        Async,
        AsyncGeneric
    }

    private readonly record struct ControlEventInfo(
        string EventName,
        EventHandlerKind HandlerKind,
        string Accessibility,
        string EventTypeDisplay,
        string? GenericSenderTypeDisplay,
        string? GenericArgsTypeDisplay
    );

    private readonly record struct TypeParameterModel(
        string Name,
        EquatableArray<string> Constraints
    );

    private readonly record struct ControlTypeModel(
        string FullyQualifiedName,
        bool IsPublic,
        EquatableArray<TypeParameterModel> TypeParameters,
        EquatableArray<ControlEventInfo> Events
    );

    private sealed record EventModel(string Name, string EventName, string Accessibility, string PropertyType, string Assignment);

    private sealed record ExtensionSignature(string Signature, string Constraints);
}
