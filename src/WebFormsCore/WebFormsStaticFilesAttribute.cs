using System;

namespace WebFormsCore;

/// <summary>
/// Marks an assembly as containing embedded static files that should be served by WebFormsCore.
/// When <see cref="AspNetCoreExtensions.UseWebFormsCore"/> is called, assemblies with this attribute
/// will automatically have their embedded files served via <see cref="Microsoft.Extensions.FileProviders.ManifestEmbeddedFileProvider"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class WebFormsStaticFilesAttribute : Attribute
{
    /// <summary>
    /// Gets the root path within the assembly's embedded resources.
    /// </summary>
    public string Root { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebFormsStaticFilesAttribute"/> class.
    /// </summary>
    /// <param name="root">The root path within the assembly's embedded resources (e.g., "wwwroot").</param>
    public WebFormsStaticFilesAttribute(string root = "wwwroot")
    {
        Root = root;
    }
}
