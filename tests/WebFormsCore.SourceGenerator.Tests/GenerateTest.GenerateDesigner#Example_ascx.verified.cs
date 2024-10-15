//HintName: Example_ascx.cs

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable IL2072 // DynamicallyAccessedMemberTypes

[assembly:WebFormsCore.AssemblyViewAttribute(@"Example.ascx", typeof(Tests.CompiledViews_Tests.Example_ascx))]

namespace Tests
{

[WebFormsCore.ViewPath(@"Example.ascx")]
partial class ControlTest
{
    protected global::WebFormsCore.UI.WebControls.Literal litTest = default!;
    protected global::WebFormsCore.UI.WebControls.Button btnIncrement = default!;
}

public partial class CompiledViews_Tests
{
    [WebFormsCore.CompiledView(@"Example.ascx", "5D486F276D786E27DCBC39C051FA927E")]
    public class Example_ascx : global::Tests.ControlTest
    {
        public Example_ascx(global::Tests.IService service)
            : base(service)
        {
        }
        

        public override string AppFullPath => @"Example.ascx";

        public override string AppRelativeVirtualPath => @"Example.ascx";

        public override string TemplateSourceDirectory => @"";

        protected override void FrameworkInitialize()
        {
            base.FrameworkInitialize();
            
            var ctrl0 = WebActivator.CreateControl<global::WebFormsCore.UI.WebControls.Literal>();
            ctrl0.ID = "litTest";
            this.AddParsedSubObject(ctrl0);

            
            this.litTest = ctrl0;
            this.AddParsedSubObject(WebActivator.CreateLiteral("\r\n"));
            var ctrl1 = WebActivator.CreateControl<global::WebFormsCore.UI.WebControls.Button>();
            ctrl1.ID = "btnIncrement";
            this.AddParsedSubObject(ctrl1);

            
            ctrl1.Click += (sender, e) =>
            {
                this.btnIncrement_OnClick(sender, e);
                return System.Threading.Tasks.Task.CompletedTask;
            };
            this.btnIncrement = ctrl1;
            ctrl1.AddParsedSubObject(WebActivator.CreateLiteral("Increment"));
        }
    }
}

}
