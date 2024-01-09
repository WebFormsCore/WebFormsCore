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
internal sealed record ReportedDiagnostic(DiagnosticDescriptor Descriptor, string FilePath, TextSpan TextSpan, LinePositionSpan LineSpan)
{
    /// <summary>
    /// Implicitly converts <see cref="ReportedDiagnostic"/> to <see cref="Diagnostic"/>.
    /// </summary>
    /// <param name="diagnostic">Diagnostic to convert.</param>
    public static implicit operator Diagnostic(ReportedDiagnostic diagnostic)
    {
        return Diagnostic.Create(
            descriptor: diagnostic.Descriptor,
            location: Location.Create(diagnostic.FilePath, diagnostic.TextSpan, diagnostic.LineSpan));
    }

    /// <summary>
    /// Creates a new <see cref="ReportedDiagnostic"/> from <see cref="DiagnosticDescriptor"/> and <see cref="Location"/>.
    /// </summary>
    /// <param name="descriptor">Descriptor.</param>
    /// <param name="location">Location.</param>
    /// <returns>A new <see cref="ReportedDiagnostic"/>.</returns>
    public static ReportedDiagnostic Create(DiagnosticDescriptor descriptor, Location location)
    {
        return new(descriptor, location.SourceTree?.FilePath ?? string.Empty, location.SourceSpan, location.GetLineSpan().Span);
    }
}