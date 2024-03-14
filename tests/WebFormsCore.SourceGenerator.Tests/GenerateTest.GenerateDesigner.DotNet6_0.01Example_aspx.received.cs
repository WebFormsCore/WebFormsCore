//HintName: Example_aspx.cs

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable IL2072 // DynamicallyAccessedMemberTypes

[assembly:WebFormsCore.AssemblyViewAttribute(@"Example.aspx", typeof(Tests.CompiledViews_Tests.Example_aspx))]

namespace Tests
{

[WebFormsCore.ViewPath(@"Example.aspx")]
partial class PageTest
{
    protected global::WebFormsCore.UI.WebControls.TextBox tbUsername = default!;
    protected global::WebFormsCore.UI.WebControls.TextBox tbPassword = default!;
    protected global::WebFormsCore.UI.WebControls.Button btnLogin = default!;
}

public partial class CompiledViews_Tests
{
    [WebFormsCore.CompiledView(@"Example.aspx", "54923F66579973C927C298D45C4619DF")]
    public class Example_aspx : PageTest
    {
        public Example_aspx()
            : base()
        {
        }
        

        public override string AppFullPath => @"Example.aspx";

        public override string AppRelativeVirtualPath => @"Example.aspx";

        public override string TemplateSourceDirectory => @"";

        private async System.Threading.Tasks.Task Render_0(WebFormsCore.UI.HtmlTextWriter writer, WebFormsCore.UI.ControlCollection controls, System.Threading.CancellationToken token)
        {
            await controls[0].RenderAsync(writer, token); // Text: "\r\n    "
            await writer.WriteObjectAsync(
#line 9 "{CurrentDirectory}Example.aspx"
        Test 
#line default
            , false);
            await controls[1].RenderAsync(writer, token); // Text: "\r\n\r\n    "
            await controls[2].RenderAsync(writer, token); // Element: form
            await controls[3].RenderAsync(writer, token); // Text: "\r\n"
        }

        protected override void FrameworkInitialize()
        {
            base.FrameworkInitialize();
            
            this.EnableViewState = WebActivator.ParseAttribute<bool>("false");
            this.AddParsedSubObject(WebActivator.CreateLiteral("\r\n"));
            this.AddParsedSubObject(WebActivator.CreateLiteral("\r\n"));
            this.AddParsedSubObject(WebActivator.CreateLiteral("<!DOCTYPE htm>"));
            this.AddParsedSubObject(WebActivator.CreateLiteral("\r\n"));
            var ctrl0 = WebActivator.CreateElement("html");
            this.AddParsedSubObject(ctrl0);
            ctrl0.AddParsedSubObject(WebActivator.CreateLiteral("\r\n"));
            var ctrl1 = WebActivator.CreateControl<global::WebFormsCore.UI.HtmlControls.HtmlHead>();
            ctrl0.AddParsedSubObject(ctrl1);

            
            ctrl1.AddParsedSubObject(WebActivator.CreateLiteral("\r\n    "));
            var ctrl2 = WebActivator.CreateElement("title");
            ctrl1.AddParsedSubObject(ctrl2);
            ctrl1.AddParsedSubObject(WebActivator.CreateLiteral("\r\n"));
            ctrl0.AddParsedSubObject(WebActivator.CreateLiteral("\r\n"));
            var ctrl3 = WebActivator.CreateElement("body");
            ctrl0.AddParsedSubObject(ctrl3);
            ctrl3.SetRenderMethodDelegate(Render_0);
            ctrl3.AddParsedSubObject(WebActivator.CreateLiteral("\r\n    "));
            // Unhandled type: Expression
            ctrl3.AddParsedSubObject(WebActivator.CreateLiteral("\r\n\r\n    "));
            var ctrl4 = WebActivator.CreateControl<global::WebFormsCore.UI.HtmlControls.HtmlForm>();
            ctrl4.ID = "form1";
            ctrl3.AddParsedSubObject(ctrl4);

            
            this.form1 = ctrl4;
            ctrl4.AddParsedSubObject(WebActivator.CreateLiteral("\r\n        <div>\r\n            "));
            var ctrl5 = WebActivator.CreateControl<global::WebFormsCore.UI.WebControls.TextBox>();
            ctrl5.ID = "tbUsername";
            ctrl4.AddParsedSubObject(ctrl5);

            
            ctrl5.Text =  Test ;
            this.tbUsername = ctrl5;
            ctrl4.AddParsedSubObject(WebActivator.CreateLiteral("<br />\r\n            "));
            var ctrl6 = WebActivator.CreateControl<global::WebFormsCore.UI.WebControls.TextBox>();
            ctrl6.ID = "tbPassword";
            ctrl4.AddParsedSubObject(ctrl6);

            
            this.tbPassword = ctrl6;
            ctrl4.AddParsedSubObject(WebActivator.CreateLiteral("<br />\r\n            "));
            var ctrl7 = WebActivator.CreateControl<global::WebFormsCore.UI.WebControls.Button>();
            ctrl7.ID = "btnLogin";
            ctrl4.AddParsedSubObject(ctrl7);

            
            ctrl7.Text = "Login";
            ((WebFormsCore.UI.IAttributeAccessor)ctrl7).SetAttribute("click", "btnLogin_Click");
            this.btnLogin = ctrl7;
            ctrl4.AddParsedSubObject(WebActivator.CreateLiteral("\r\n        </div>\r\n    "));
            ctrl3.AddParsedSubObject(WebActivator.CreateLiteral("\r\n"));
            ctrl0.AddParsedSubObject(WebActivator.CreateLiteral("\r\n"));
        }
    }
}

}