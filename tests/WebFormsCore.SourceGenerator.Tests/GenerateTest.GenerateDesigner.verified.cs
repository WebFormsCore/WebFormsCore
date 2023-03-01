//HintName: WebForms.Designer.cs
#line hidden

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
        #line hidden
        
        #line 1 "Example.aspx"
            this.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
        #line hidden
        
        #line 2 "Example.aspx"
            // Unhandled type: Directive
        #line hidden
        
        #line 2 "Example.aspx"
            this.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
        #line hidden
        
        #line 3 "Example.aspx"
            this.AddParsedSubObject(WebActivator.CreateLiteral("<!DOCTYPE htm>"));
        #line hidden
        
        #line 3 "Example.aspx"
            this.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
        #line hidden
        
        #line 4 "Example.aspx"
            var ctrl0 = WebActivator.CreateElement("html");
            this.AddParsedSubObject(ctrl0);
        #line hidden
        
        #line 4 "Example.aspx"
            ctrl0.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
        #line hidden
        
        #line 5 "Example.aspx"
            var ctrl1 =  WebActivator.CreateControl<global::WebFormsCore.UI.HtmlControls.HtmlGenericControl>();
            ctrl0.AddParsedSubObject(ctrl1);
            ctrl1.TagName = WebActivator.ParseAttribute<string>("head");
        #line hidden
        
        #line 5 "Example.aspx"
            ctrl1.AddParsedSubObject(WebActivator.CreateLiteral("\n    "));
        #line hidden
        
        #line 6 "Example.aspx"
            var ctrl2 = WebActivator.CreateElement("title");
            ctrl1.AddParsedSubObject(ctrl2);
        #line hidden
        
        #line 6 "Example.aspx"
            ctrl1.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
        #line hidden
        
        #line 7 "Example.aspx"
            ctrl0.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
        #line hidden
        
        #line 8 "Example.aspx"
            var ctrl3 = WebActivator.CreateElement("body");
            ctrl0.AddParsedSubObject(ctrl3);
            ctrl3.SetRenderMethodDelegate(Render_0);
        #line hidden
        
        #line 8 "Example.aspx"
            ctrl3.AddParsedSubObject(WebActivator.CreateLiteral("\n    "));
        #line hidden
        
        #line 9 "Example.aspx"
            var ctrl4 =  WebActivator.CreateControl<global::WebFormsCore.UI.HtmlControls.HtmlScript>();
            ctrl3.AddParsedSubObject(ctrl4);
        #line hidden
        
        #line 9 "Example.aspx"
            ctrl4.AddParsedSubObject(WebActivator.CreateLiteral("\n        private string Test => \"Test\";\n    "));
        #line hidden
        
        #line 11 "Example.aspx"
            ctrl3.AddParsedSubObject(WebActivator.CreateLiteral("\n\n    "));
        #line hidden
        
        #line 13 "Example.aspx"
            // Unhandled type: Expression
        #line hidden
        
        #line 13 "Example.aspx"
            ctrl3.AddParsedSubObject(WebActivator.CreateLiteral("\n\n    "));
        #line hidden
        
        #line 15 "Example.aspx"
            var ctrl5 =  WebActivator.CreateControl<global::WebFormsCore.UI.HtmlControls.HtmlForm>();
            ctrl3.AddParsedSubObject(ctrl5);
            ctrl5.ID = WebActivator.ParseAttribute<string>("form1");
            this.form1 = ctrl5;
        #line hidden
        
        #line 15 "Example.aspx"
            ctrl5.AddParsedSubObject(WebActivator.CreateLiteral("\n        "));
        #line hidden
        
        #line 16 "Example.aspx"
            var ctrl6 = WebActivator.CreateElement("div");
            ctrl5.AddParsedSubObject(ctrl6);
        #line hidden
        
        #line 16 "Example.aspx"
            ctrl6.AddParsedSubObject(WebActivator.CreateLiteral("\n            "));
        #line hidden
        
        #line 17 "Example.aspx"
            var ctrl7 =  WebActivator.CreateControl<global::Tests.ControlTest<global::Tests.TestItem>>();
            ctrl6.AddParsedSubObject(ctrl7);
            ((WebFormsCore.UI.IAttributeAccessor)ctrl7).SetAttribute("ItemType", "Tests.TestItem");
            ctrl7.Template = new Template_PageTest_ctrl7_Template(WebActivator, this);
        #line hidden
        
        #line 17 "Example.aspx"
            ctrl7.AddParsedSubObject(WebActivator.CreateLiteral("\n                "));
        #line hidden
        
        #line 18 "Example.aspx"
            ctrl7.AddParsedSubObject(WebActivator.CreateLiteral("\n            "));
        #line hidden
        
        #line 19 "Example.aspx"
            var ctrl9 = WebActivator.CreateElement("br");
            ctrl6.AddParsedSubObject(ctrl9);
        #line hidden
        
        #line 19 "Example.aspx"
            ctrl6.AddParsedSubObject(WebActivator.CreateLiteral("\n            "));
        #line hidden
        
        #line 20 "Example.aspx"
            var ctrl10 =  WebActivator.CreateControl<global::WebFormsCore.UI.WebControls.TextBox>();
            ctrl6.AddParsedSubObject(ctrl10);
            ctrl10.ID = WebActivator.ParseAttribute<string>("tbUsername");
            this.tbUsername = ctrl10;
        #line hidden
        
        #line 20 "Example.aspx"
            var ctrl11 = WebActivator.CreateElement("br");
            ctrl6.AddParsedSubObject(ctrl11);
        #line hidden
        
        #line 20 "Example.aspx"
            ctrl6.AddParsedSubObject(WebActivator.CreateLiteral("\n            "));
        #line hidden
        
        #line 21 "Example.aspx"
            var ctrl12 =  WebActivator.CreateControl<global::WebFormsCore.UI.WebControls.TextBox>();
            ctrl6.AddParsedSubObject(ctrl12);
            ctrl12.ID = WebActivator.ParseAttribute<string>("tbPassword");
            this.tbPassword = ctrl12;
        #line hidden
        
        #line 21 "Example.aspx"
            var ctrl13 = WebActivator.CreateElement("br");
            ctrl6.AddParsedSubObject(ctrl13);
        #line hidden
        
        #line 21 "Example.aspx"
            ctrl6.AddParsedSubObject(WebActivator.CreateLiteral("\n            "));
        #line hidden
        
        #line 22 "Example.aspx"
            var ctrl14 =  WebActivator.CreateControl<global::WebFormsCore.UI.WebControls.Button>();
            ctrl6.AddParsedSubObject(ctrl14);
            ctrl14.ID = WebActivator.ParseAttribute<string>("btnLogin");
            ctrl14.Text = WebActivator.ParseAttribute<string>("Login");
            ((WebFormsCore.UI.IAttributeAccessor)ctrl14).SetAttribute("click", "btnLogin_Click");
            this.btnLogin = ctrl14;
        #line hidden
        
        #line 22 "Example.aspx"
            ctrl6.AddParsedSubObject(WebActivator.CreateLiteral("\n        "));
        #line hidden
        
        #line 23 "Example.aspx"
            ctrl5.AddParsedSubObject(WebActivator.CreateLiteral("\n    "));
        #line hidden
        
        #line 24 "Example.aspx"
            ctrl3.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
        #line hidden
        
        #line 25 "Example.aspx"
            ctrl0.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
        #line hidden
        }
        public class Template_PageTest_ctrl7_Template : WebFormsCore.UI.ITemplate
        {
            private readonly Example_aspx _parent;

            public Template_PageTest_ctrl7_Template(WebFormsCore.IWebObjectActivator webActivator, Example_aspx parent)
            {
                WebActivator = webActivator;
                _parent = parent;
            }

            public WebFormsCore.IWebObjectActivator WebActivator { get; }

            public void InstantiateIn(WebFormsCore.UI.Control container)
            {
                
        
        #line 18 "Example.aspx"
            container.AddParsedSubObject(WebActivator.CreateLiteral("Test"));
        #line hidden
            }
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
        public Example_ascx(global::Tests.IService service)
            : base(service)
        {
        }
        

        protected override void FrameworkInitialize()
        {
            base.FrameworkInitialize();
            
        
        #line 1 "Example.ascx"
            // Unhandled type: Directive
        #line hidden
        
        #line 1 "Example.ascx"
            this.AddParsedSubObject(WebActivator.CreateLiteral("\n\n"));
        #line hidden
        
        #line 3 "Example.ascx"
            var ctrl0 =  WebActivator.CreateControl<global::WebFormsCore.UI.WebControls.Literal>();
            this.AddParsedSubObject(ctrl0);
            ctrl0.ID = WebActivator.ParseAttribute<string>("litTest");
            this.litTest = ctrl0;
        #line hidden
        
        #line 3 "Example.ascx"
            this.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
        #line hidden
        
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
        #line hidden
        
        #line 4 "Example.ascx"
            ctrl1.AddParsedSubObject(WebActivator.CreateLiteral("Increment"));
        #line hidden
        }
    }
}
}