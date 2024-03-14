//HintName: GenerateAssemblyTypeProvider.cs
namespace Tests
{

internal class AssemblyControlTypeProvider : WebFormsCore.IControlTypeProvider
{
    public System.Collections.Generic.Dictionary<string, System.Type> GetTypes()
    {
        return new System.Collections.Generic.Dictionary<string, System.Type>
        {
            { "Example.aspx", typeof(global::Tests.CompiledViews_Tests.Example_aspx) },
            { "Example.ascx", typeof(global::Tests.CompiledViews_Tests.Example_ascx) },
        };
    }
}

}
