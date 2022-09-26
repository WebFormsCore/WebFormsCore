//HintName: WebForms.Designer.cs

[assembly:WebFormsCore.RootNamespaceAttribute("Tests")]
namespace Tests
{
[WebFormsCore.ViewPath(@"Example.aspx")]
partial class PageTest
{
    protected global::WebFormsCore.UI.HtmlControls.HtmlForm form1;
    protected global::WebFormsCore.UI.WebControls.TextBox tbUsername;
    protected global::WebFormsCore.UI.WebControls.TextBox tbPassword;
    protected global::WebFormsCore.UI.WebControls.Button btnLogin;
}

public partial class CompiledViews
{
    public class Template_PageTest_ctrl7_Template : WebFormsCore.UI.ITemplate
    {
        public Template_PageTest_ctrl7_Template(WebFormsCore.IWebObjectActivator webActivator)
        {
            WebActivator = webActivator;
        }

        public WebFormsCore.IWebObjectActivator WebActivator { get; }

        public void InstantiateIn(WebFormsCore.UI.Control container)
        {
            
            #line 18 "Example.aspx"
            container.AddParsedSubObject(WebActivator.CreateLiteral("Test"));
            #line default
        }
    }

    [WebFormsCore.CompiledView(@"Example.aspx", "22DDCDE287F9E65746E03EDFC747C09D")]
    public class Example_aspx : PageTest
    {

        private async System.Threading.Tasks.Task Render_0(WebFormsCore.UI.HtmlTextWriter writer, WebFormsCore.UI.ControlCollection controls, System.Threading.CancellationToken token)
        {
            #line 8 "Example.aspx"
            await controls[0].RenderAsync(writer, token); // Text: "\n    "
            #line default
            #line 9 "Example.aspx"
            await controls[1].RenderAsync(writer, token); // Element: script
            #line default
            #line 11 "Example.aspx"
            await controls[2].RenderAsync(writer, token); // Text: "\n\n    "
            #line default
            #line 13 "Example.aspx"
            await writer.WriteObjectAsync( Test );
            #line default
            #line 13 "Example.aspx"
            await controls[3].RenderAsync(writer, token); // Text: "\n\n    "
            #line default
            #line 15 "Example.aspx"
            await controls[4].RenderAsync(writer, token); // Element: form
            #line default
            #line 24 "Example.aspx"
            await controls[5].RenderAsync(writer, token); // Text: "\n"
            #line default
        }

        protected override void FrameworkInitialize()
        {
            base.FrameworkInitialize();
            
            #line 1 "Example.aspx"
            // Unhandled type: Directive
            #line default
            #line 1 "Example.aspx"
            this.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
            #line default
            #line 2 "Example.aspx"
            // Unhandled type: Directive
            #line default
            #line 2 "Example.aspx"
            this.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
            #line default
            #line 3 "Example.aspx"
            this.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
            #line default
            #line 4 "Example.aspx"
            var ctrl0 = WebActivator.CreateElement("html");
            this.AddParsedSubObject(ctrl0);
            #line default
            #line 4 "Example.aspx"
            ctrl0.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
            #line default
            #line 5 "Example.aspx"
            var ctrl1 =  WebActivator.CreateControl<global::WebFormsCore.UI.HtmlControls.HtmlGenericControl>();
            ctrl0.AddParsedSubObject(ctrl1);
            ctrl1.TagName = WebActivator.ParseAttribute<string>("head");
            #line default
            #line 5 "Example.aspx"
            ctrl1.AddParsedSubObject(WebActivator.CreateLiteral("\n    "));
            #line default
            #line 6 "Example.aspx"
            var ctrl2 = WebActivator.CreateElement("title");
            ctrl1.AddParsedSubObject(ctrl2);
            #line default
            #line 6 "Example.aspx"
            ctrl1.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
            #line default
            #line 7 "Example.aspx"
            ctrl0.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
            #line default
            #line 8 "Example.aspx"
            var ctrl3 = WebActivator.CreateElement("body");
            ctrl0.AddParsedSubObject(ctrl3);
            ctrl3.SetRenderMethodDelegate(Render_0);
            #line default
            #line 8 "Example.aspx"
            ctrl3.AddParsedSubObject(WebActivator.CreateLiteral("\n    "));
            #line default
            #line 9 "Example.aspx"
            var ctrl4 =  WebActivator.CreateControl<global::WebFormsCore.UI.HtmlControls.HtmlGenericControl>();
            ctrl3.AddParsedSubObject(ctrl4);
            ctrl4.TagName = WebActivator.ParseAttribute<string>("script");
            #line default
            #line 9 "Example.aspx"
            ctrl4.AddParsedSubObject(WebActivator.CreateLiteral("\n        private string Test => \"Test\";\n    "));
            #line default
            #line 11 "Example.aspx"
            ctrl3.AddParsedSubObject(WebActivator.CreateLiteral("\n\n    "));
            #line default
            #line 13 "Example.aspx"
            // Unhandled type: Expression
            #line default
            #line 13 "Example.aspx"
            ctrl3.AddParsedSubObject(WebActivator.CreateLiteral("\n\n    "));
            #line default
            #line 15 "Example.aspx"
            var ctrl5 =  WebActivator.CreateControl<global::WebFormsCore.UI.HtmlControls.HtmlForm>();
            ctrl3.AddParsedSubObject(ctrl5);
            ctrl5.ID = WebActivator.ParseAttribute<string>("form1");
            this.form1 = ctrl5;
            #line default
            #line 15 "Example.aspx"
            ctrl5.AddParsedSubObject(WebActivator.CreateLiteral("\n        "));
            #line default
            #line 16 "Example.aspx"
            var ctrl6 = WebActivator.CreateElement("div");
            ctrl5.AddParsedSubObject(ctrl6);
            #line default
            #line 16 "Example.aspx"
            ctrl6.AddParsedSubObject(WebActivator.CreateLiteral("\n            "));
            #line default
            #line 17 "Example.aspx"
            var ctrl7 =  WebActivator.CreateControl<global::Tests.ControlTest<global::Tests.TestItem>>();
            ctrl6.AddParsedSubObject(ctrl7);
            ctrl7.Attributes.Add("ItemType", "Tests.TestItem");
            ctrl7.Template = new Template_PageTest_ctrl7_Template(WebActivator);
            #line default
            #line 17 "Example.aspx"
            ctrl7.AddParsedSubObject(WebActivator.CreateLiteral("\n                "));
            #line default
            #line 18 "Example.aspx"
            ctrl7.AddParsedSubObject(WebActivator.CreateLiteral("\n            "));
            #line default
            #line 19 "Example.aspx"
            var ctrl9 = WebActivator.CreateElement("br");
            ctrl6.AddParsedSubObject(ctrl9);
            #line default
            #line 19 "Example.aspx"
            ctrl6.AddParsedSubObject(WebActivator.CreateLiteral("\n            "));
            #line default
            #line 20 "Example.aspx"
            var ctrl10 =  WebActivator.CreateControl<global::WebFormsCore.UI.WebControls.TextBox>();
            ctrl6.AddParsedSubObject(ctrl10);
            ctrl10.ID = WebActivator.ParseAttribute<string>("tbUsername");
            this.tbUsername = ctrl10;
            #line default
            #line 20 "Example.aspx"
            var ctrl11 = WebActivator.CreateElement("br");
            ctrl6.AddParsedSubObject(ctrl11);
            #line default
            #line 20 "Example.aspx"
            ctrl6.AddParsedSubObject(WebActivator.CreateLiteral("\n            "));
            #line default
            #line 21 "Example.aspx"
            var ctrl12 =  WebActivator.CreateControl<global::WebFormsCore.UI.WebControls.TextBox>();
            ctrl6.AddParsedSubObject(ctrl12);
            ctrl12.ID = WebActivator.ParseAttribute<string>("tbPassword");
            this.tbPassword = ctrl12;
            #line default
            #line 21 "Example.aspx"
            var ctrl13 = WebActivator.CreateElement("br");
            ctrl6.AddParsedSubObject(ctrl13);
            #line default
            #line 21 "Example.aspx"
            ctrl6.AddParsedSubObject(WebActivator.CreateLiteral("\n            "));
            #line default
            #line 22 "Example.aspx"
            var ctrl14 =  WebActivator.CreateControl<global::WebFormsCore.UI.WebControls.Button>();
            ctrl6.AddParsedSubObject(ctrl14);
            ctrl14.ID = WebActivator.ParseAttribute<string>("btnLogin");
            ctrl14.Attributes.Add("click", "btnLogin_Click");
            ctrl14.Attributes.Add("text", "Login");
            this.btnLogin = ctrl14;
            #line default
            #line 22 "Example.aspx"
            ctrl6.AddParsedSubObject(WebActivator.CreateLiteral("\n        "));
            #line default
            #line 23 "Example.aspx"
            ctrl5.AddParsedSubObject(WebActivator.CreateLiteral("\n    "));
            #line default
            #line 24 "Example.aspx"
            ctrl3.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
            #line default
            #line 25 "Example.aspx"
            ctrl0.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
            #line default
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

    [WebFormsCore.CompiledView(@"Example.ascx", "E2A364FA5B80EF5EA10E7D05559FEB3E")]
    public class Example_ascx : ControlTest
    {

        protected override void FrameworkInitialize()
        {
            base.FrameworkInitialize();
            
            #line 1 "Example.ascx"
            // Unhandled type: Directive
            #line default
            #line 1 "Example.ascx"
            this.AddParsedSubObject(WebActivator.CreateLiteral("\n\n"));
            #line default
            #line 3 "Example.ascx"
            var ctrl0 =  WebActivator.CreateControl<global::WebFormsCore.UI.WebControls.Literal>();
            this.AddParsedSubObject(ctrl0);
            ctrl0.ID = WebActivator.ParseAttribute<string>("litTest");
            this.litTest = ctrl0;
            #line default
            #line 3 "Example.ascx"
            this.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
            #line default
            #line 4 "Example.ascx"
            var ctrl1 =  WebActivator.CreateControl<global::WebFormsCore.UI.WebControls.Button>();
            this.AddParsedSubObject(ctrl1);
            ctrl1.Click += (sender, e) =>
            {
                btnIncrement_OnClick(sender, e);
                return System.Threading.Tasks.Task.CompletedTask;
            };
            ctrl1.ID = WebActivator.ParseAttribute<string>("btnIncrement");
            this.btnIncrement = ctrl1;
            #line default
            #line 4 "Example.ascx"
            ctrl1.AddParsedSubObject(WebActivator.CreateLiteral("Increment"));
            #line default
        }
    }
}
}