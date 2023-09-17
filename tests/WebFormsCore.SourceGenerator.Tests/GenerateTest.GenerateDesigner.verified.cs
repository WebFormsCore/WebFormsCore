//HintName: WebForms.Designer.cs
#line hidden

[assembly:WebFormsCore.RootNamespaceAttribute("Tests")]
[assembly:WebFormsCore.AssemblyViewAttribute(@"Example.aspx", typeof(Tests.CompiledViews.Example_aspx))]
[assembly:WebFormsCore.AssemblyViewAttribute(@"Example.ascx", typeof(Tests.CompiledViews.Example_ascx))]


namespace Tests
{
[WebFormsCore.ViewPath(@"Example.aspx")]
partial class PageTest
{
    protected global::WebFormsCore.UI.WebControls.TextBox tbUsername;
    protected global::WebFormsCore.UI.WebControls.TextBox tbPassword;
    protected global::WebFormsCore.UI.WebControls.Button btnLogin;
}

public partial class CompiledViews
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
            await controls[0].RenderAsync(writer, token); // Text: "\n    "
            await writer.WriteObjectAsync(
#line 9 "{CurrentDirectory}Example.aspx"
        Test 
#line hidden
            , false);
            await controls[1].RenderAsync(writer, token); // Text: "\n\n    "
            await controls[2].RenderAsync(writer, token); // Element: form
            await controls[3].RenderAsync(writer, token); // Text: "\n"
        }

        protected override void FrameworkInitialize()
        {
            base.FrameworkInitialize();
            
            this.EnableViewState = WebActivator.ParseAttribute<bool>("false");
            this.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
            this.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
            this.AddParsedSubObject(WebActivator.CreateLiteral("<!DOCTYPE htm>"));
            this.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
            var ctrl0 = WebActivator.CreateElement("html");
            this.AddParsedSubObject(ctrl0);
            ctrl0.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
            var ctrl1 = WebActivator.CreateControl<global::WebFormsCore.UI.HtmlControls.HtmlHead>();
            ctrl0.AddParsedSubObject(ctrl1);

            
            ctrl1.AddParsedSubObject(WebActivator.CreateLiteral("\n    "));
            var ctrl2 = WebActivator.CreateElement("title");
            ctrl1.AddParsedSubObject(ctrl2);
            ctrl1.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
            ctrl0.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
            var ctrl3 = WebActivator.CreateElement("body");
            ctrl0.AddParsedSubObject(ctrl3);
            ctrl3.SetRenderMethodDelegate(Render_0);
            ctrl3.AddParsedSubObject(WebActivator.CreateLiteral("\n    "));
            // Unhandled type: Expression
            ctrl3.AddParsedSubObject(WebActivator.CreateLiteral("\n\n    "));
            var ctrl4 = WebActivator.CreateControl<global::WebFormsCore.UI.HtmlControls.HtmlForm>();
            ctrl4.ID = "form1";
            ctrl3.AddParsedSubObject(ctrl4);

            
            this.form1 = ctrl4;
            ctrl4.AddParsedSubObject(WebActivator.CreateLiteral("\n        <div>\n            "));
            var ctrl5 = WebActivator.CreateControl<global::WebFormsCore.UI.WebControls.TextBox>();
            ctrl5.ID = "tbUsername";
            ctrl4.AddParsedSubObject(ctrl5);

            
            ctrl5.Text =  Test ;
            this.tbUsername = ctrl5;
            ctrl4.AddParsedSubObject(WebActivator.CreateLiteral("<br />\n            "));
            var ctrl6 = WebActivator.CreateControl<global::WebFormsCore.UI.WebControls.TextBox>();
            ctrl6.ID = "tbPassword";
            ctrl4.AddParsedSubObject(ctrl6);

            
            this.tbPassword = ctrl6;
            ctrl4.AddParsedSubObject(WebActivator.CreateLiteral("<br />\n            "));
            var ctrl7 = WebActivator.CreateControl<global::WebFormsCore.UI.WebControls.Button>();
            ctrl7.ID = "btnLogin";
            ctrl4.AddParsedSubObject(ctrl7);

            
            ctrl7.Text = "Login";
            ((WebFormsCore.UI.IAttributeAccessor)ctrl7).SetAttribute("click", "btnLogin_Click");
            this.btnLogin = ctrl7;
            ctrl4.AddParsedSubObject(WebActivator.CreateLiteral("\n        </div>\n    "));
            ctrl3.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
            ctrl0.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
        }
    }
}
}
namespace Tests
{
[WebFormsCore.ViewPath(@"Example.ascx")]
partial class ControlTest
{
    protected global::WebFormsCore.UI.WebControls.Literal litTest;
    protected global::WebFormsCore.UI.WebControls.Button btnIncrement;
}

public partial class CompiledViews
{
    [WebFormsCore.CompiledView(@"Example.ascx", "5D486F276D786E27DCBC39C051FA927E")]
    public class Example_ascx : ControlTest
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
            
            this.AddParsedSubObject(WebActivator.CreateLiteral("\n\n"));
            var ctrl0 = WebActivator.CreateControl<global::WebFormsCore.UI.WebControls.Literal>();
            ctrl0.ID = "litTest";
            this.AddParsedSubObject(ctrl0);

            
            this.litTest = ctrl0;
            this.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
            var ctrl1 = WebActivator.CreateControl<global::WebFormsCore.UI.WebControls.Button>();
            ctrl1.ID = "btnIncrement";
            this.AddParsedSubObject(ctrl1);

            
            ctrl1.Click += (sender, e) =>
            {
                btnIncrement_OnClick(sender, e);
                return System.Threading.Tasks.Task.CompletedTask;
            };
            this.btnIncrement = ctrl1;
            ctrl1.AddParsedSubObject(WebActivator.CreateLiteral("Increment"));
        }
    }
}
}