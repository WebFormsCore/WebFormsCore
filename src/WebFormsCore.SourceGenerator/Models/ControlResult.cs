namespace WebFormsCore.SourceGenerator.Models;

internal record ControlResult(
    EquatableArray<ReportedDiagnostic> Diagnostics = default,
    ControlResultContext? Context = default
);

public record ControlResultContext(
    string? RootNamespace,
    string? Namespace,
    string ClassName,
    string Content,
    string RelativePath,
    string CompiledViewType,
    string Code
);

public record ControlType(
    string? RootNamespace,
    string RelativePath,
    string CompiledViewType
);