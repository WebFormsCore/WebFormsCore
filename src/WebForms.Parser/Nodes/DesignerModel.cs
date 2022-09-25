namespace WebFormsCore.Nodes;

public record DesignerModel(List<RootNode> Types, string? RootNamespace, bool AddFields = true);
