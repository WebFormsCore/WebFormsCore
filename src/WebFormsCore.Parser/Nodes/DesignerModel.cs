namespace WebFormsCore.Nodes;

public record DesignerModel(IReadOnlyList<RootNode> Types, string? RootNamespace, bool AddFields = true);
