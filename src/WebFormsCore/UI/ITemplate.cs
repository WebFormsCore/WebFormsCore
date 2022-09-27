namespace WebFormsCore.UI;

/// <summary>
/// Defines the behavior for populating a templated ASP.NET server control with child controls. The child controls represent the inline templates defined on the page.
/// </summary>
public interface ITemplate
{
    /// <summary>When implemented by a class, defines the <see cref="T:WebFormsCore.UI.Control" /> object that child controls and templates belong to. These child controls are in turn defined within an inline template.</summary>
    /// <param name="container">The <see cref="T:WebFormsCore.UI.Control" /> object to contain the instances of controls from the inline template. </param>
    void InstantiateIn(Control container);
}
