using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace WebFormsCore.SourceGenerator.Models;

/// <summary>
/// Basic diagnostic description for reporting diagnostic inside the incremental pipeline.
/// </summary>
/// <param name="Descriptor">Diagnostic descriptor.</param>
/// <param name="FilePath">File path.</param>
/// <param name="TextSpan">Text span.</param>
/// <param name="LineSpan">Line span.</param>
/// <see href="https://github.com/dotnet/roslyn/issues/62269#issuecomment-1170760367" />
public sealed record ReportedDiagnostic(
    DiagnosticDescriptor Descriptor,
    TextSpan TextSpan,
    FileLinePositionSpan FileLineSpan,
    EquatableArray<object> Arguments)
{
    /// <summary>
    /// Implicitly converts <see cref="ReportedDiagnostic"/> to <see cref="Diagnostic"/>.
    /// </summary>
    /// <param name="diagnostic">Diagnostic to convert.</param>
    public static implicit operator Diagnostic(ReportedDiagnostic diagnostic)
    {
        return Diagnostic.Create(
            descriptor: diagnostic.Descriptor,
            location: Location.Create(diagnostic.FileLineSpan.Path, diagnostic.TextSpan, diagnostic.FileLineSpan.Span),
            messageArgs: diagnostic.Arguments.GetUnsafeArray());
    }

    /// <summary>
    /// Creates a new <see cref="ReportedDiagnostic"/> from <see cref="DiagnosticDescriptor"/> and <see cref="Location"/>.
    /// </summary>
    /// <param name="descriptor">Descriptor.</param>
    /// <param name="location">Location.</param>
    /// <param name="arguments">Arguments.</param>
    /// <returns>A new <see cref="ReportedDiagnostic"/>.</returns>
    public static ReportedDiagnostic Create(DiagnosticDescriptor descriptor, Location location, params object[] arguments)
    {
        return new(descriptor, location.SourceSpan, location.GetLineSpan(), arguments.ToImmutableArray());
    }
}