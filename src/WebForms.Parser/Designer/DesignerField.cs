namespace WebFormsCore.Designer;

public record DesignerField(string Name, string Type, bool Assign, bool AddToDesigner);

public record DesignerEvent(string Name, string Type);