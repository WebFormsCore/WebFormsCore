using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.Features;

namespace WebFormsCore.UI;

public abstract partial class ClientDependencyInclude : Control, IClientDependencyFile
{
    protected ClientDependencyInclude()
    {
        Priority = Constants.DefaultPriority;
        Group = Constants.DefaultGroup;
        Attributes = new AttributeCollection();
        AddTag = true;
    }

    protected ClientDependencyInclude(IClientDependencyFile file)
    {
        Priority = file.Priority;
        PathNameAlias = file.PathNameAlias;
        FilePath = file.FilePath;
        DependencyType = file.DependencyType;
        Group = file.Group;
        Attributes = new AttributeCollection();
        AddTag = true;
        Name = file.Name;
        Version = file.Version;
        ForceVersion = false;
    }

    protected override void OnInit(EventArgs args)
    {
        base.OnInit(args);

        var service = Context.RequestServices.GetService<IClientDependencyCollection>();

        if (service is null)
        {
            throw new NullReferenceException($"Client dependency services are not registered. Please call builder.{nameof(ClientResourceManagementExtensions.AddClientResourceManagement)}() in your Startup.cs");
        }

        service.Add(this);
    }

    protected override void OnUnload(EventArgs args)
    {
        var service = Context.RequestServices.GetService<IClientDependencyCollection>();
        service?.Remove(this);
    }

    public ClientDependencyType DependencyType { get; internal set; }
    [ViewState] public string? FilePath { get; set; }
    [ViewState] public string? PathNameAlias { get; set; }
    [ViewState] public int Priority { get; set; }
    [ViewState] public int Group { get; set; }
    [ViewState] public bool AddTag { get; set; }

    /// <summary>Name of the script (e.g. <c>jQuery</c>, <c>Bootstrap</c>, <c>Angular</c>, etc.</summary>
    [ViewState] public string? Name { get; set; }

    /// <summary>Version of this resource if it is a named resources. Note this field is only used when <see cref="Name" /> is specified</summary>
    [ViewState] public string? Version { get; set; }

    /// <summary>Force this version to be used. Meant for skin designers that wish to override choices made by module developers or the framework.</summary>
    [ViewState] public bool ForceVersion { get; set; }

    /// <summary>
    /// This can be empty and will use default provider
    /// </summary>
    [ViewState] public string? ForceProvider { get; set; }

    /// <summary>
    /// If the resources is an external resource then normally it will be rendered as it's own download unless
    /// this is set to true. In that case the system will download the external resource and include it in the local bundle.
    /// </summary>
    [ViewState] public bool ForceBundle { get; set; }

    /// <summary>
    /// Used to store additional attributes in the HTML markup for the item
    /// </summary>
    /// <remarks>
    /// Mostly used for CSS Media, but could be for anything
    /// </remarks>
    [ViewState] public AttributeCollection Attributes { get; set; }

    /// <summary>
    /// Position of the item in the page.
    /// </summary>
    [ViewState] public ScriptPosition? Position { get; set; }

    /// <see cref="Attributes"/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public AttributeCollection HtmlAttributes => Attributes;

    /// <summary>
    /// Used to set the HtmlAttributes on this class via a string which is parsed
    /// </summary>
    /// <remarks>
    /// The syntax for the string must be: key1:value1,key2:value2   etc...
    /// </remarks>
    public string? HtmlAttributesAsString { get; set; }

    protected bool Equals(ClientDependencyInclude other)
    {
        return string.Equals(FilePath, other.FilePath, StringComparison.InvariantCultureIgnoreCase) &&
               DependencyType == other.DependencyType &&
               Priority == other.Priority &&
               Group == other.Group &&
               string.Equals(PathNameAlias, other.PathNameAlias, StringComparison.InvariantCultureIgnoreCase) &&
               string.Equals(ForceProvider, other.ForceProvider)
               && Equals(Attributes, other.Attributes);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ClientDependencyInclude)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = (FilePath != null ? FilePath.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (int)DependencyType;
            hashCode = (hashCode * 397) ^ Priority;
            hashCode = (hashCode * 397) ^ Group;
            hashCode = (hashCode * 397) ^ (PathNameAlias != null ? PathNameAlias.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (ForceProvider != null ? ForceProvider.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Attributes != null ? Attributes.GetHashCode() : 0);
            return hashCode;
        }
    }
}
