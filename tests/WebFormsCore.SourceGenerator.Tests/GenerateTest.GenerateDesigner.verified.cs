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
    [WebFormsCore.CompiledView(@"Example.aspx", "87F3495DC0937BFC7C403021323C1819")]
    public class Example_aspx : PageTest
    {
        public Example_aspx()
            : base()
        {
        }
        

        public override string AppRelativeVirtualPath => @"Example.aspx";

        public override string TemplateSourceDirectory => @"";

        private async System.Threading.Tasks.Task Render_0(WebFormsCore.UI.HtmlTextWriter writer, WebFormsCore.UI.ControlCollection controls, System.Threading.CancellationToken token)
        {
            
            #line 1 "Example.aspx"
            #line default
            
            #line 1 "Example.aspx"
            await controls[0].RenderAsync(writer, token); // Text: "\n"
            #line default
            
            #line 2 "Example.aspx"
            #line default
            
            #line 2 "Example.aspx"
            await controls[1].RenderAsync(writer, token); // Text: "\n"
            #line default
            
            #line 3 "Example.aspx"
            await controls[2].RenderAsync(writer, token); // Text: "<!DOCTYPE htm>"
            #line default
            
            #line 3 "Example.aspx"
            await controls[3].RenderAsync(writer, token); // Text: "\n<html>\n"
            #line default
            
            #line 5 "Example.aspx"
            await controls[4].RenderAsync(writer, token); // Element: head
            #line default
            
            #line 7 "Example.aspx"
            await controls[5].RenderAsync(writer, token); // Text: "\n<body>\n    "
            #line default
            
            #line 9 "Example.aspx"
            await writer.WriteObjectAsync( Test );
            #line default
            
            #line 9 "Example.aspx"
            await controls[6].RenderAsync(writer, token); // Text: "\n\n    "
            #line default
            
            #line 11 "Example.aspx"
            await controls[7].RenderAsync(writer, token); // Element: form
            #line default
            
            #line 17 "Example.aspx"
            await controls[8].RenderAsync(writer, token); // Text: "\n</body>\n</html>"
            #line default
        }

        protected override void FrameworkInitialize()
        {
            base.FrameworkInitialize();
            SetRenderMethodDelegate(Render_0);
            
        
        #line 1 "Example.aspx"
            this.EnableViewState = WebActivator.ParseAttribute<bool>("false");
        #line hidden
        
        #line 1 "Example.aspx"
            this.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
        #line hidden
        
        #line 2 "Example.aspx"
        #line hidden
        
        #line 2 "Example.aspx"
            this.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
        #line hidden
        
        #line 3 "Example.aspx"
            this.AddParsedSubObject(WebActivator.CreateLiteral("<!DOCTYPE htm>"));
        #line hidden
        
        #line 3 "Example.aspx"
            this.AddParsedSubObject(WebActivator.CreateLiteral("\n<html>\n"));
        #line hidden
        
        #line 5 "Example.aspx"
            var ctrl0 = WebActivator.CreateControl<global::WebFormsCore.UI.HtmlControls.HtmlHead>();
            this.AddParsedSubObject(ctrl0);

            
        #line hidden
        
        #line 5 "Example.aspx"
            ctrl0.AddParsedSubObject(WebActivator.CreateLiteral("\n    <title></title>\n"));
        #line hidden
        
        #line 7 "Example.aspx"
            this.AddParsedSubObject(WebActivator.CreateLiteral("\n<body>\n    "));
        #line hidden
        
        #line 9 "Example.aspx"
            // Unhandled type: Expression
        #line hidden
        
        #line 9 "Example.aspx"
            this.AddParsedSubObject(WebActivator.CreateLiteral("\n\n    "));
        #line hidden
        
        #line 11 "Example.aspx"
            var ctrl1 = WebActivator.CreateControl<global::WebFormsCore.UI.HtmlControls.HtmlForm>();
            ctrl1.ID = "form1";
            this.AddParsedSubObject(ctrl1);

            
            this.form1 = ctrl1;
        #line hidden
        
        #line 11 "Example.aspx"
            ctrl1.AddParsedSubObject(WebActivator.CreateLiteral("\n        <div>\n            "));
        #line hidden
        
        #line 13 "Example.aspx"
            var ctrl2 = WebActivator.CreateControl<global::WebFormsCore.UI.WebControls.TextBox>();
            ctrl2.ID = "tbUsername";
            ctrl1.AddParsedSubObject(ctrl2);

            
            this.tbUsername = ctrl2;
        #line hidden
        
        #line 13 "Example.aspx"
            ctrl1.AddParsedSubObject(WebActivator.CreateLiteral("<br />\n            "));
        #line hidden
        
        #line 14 "Example.aspx"
            var ctrl3 = WebActivator.CreateControl<global::WebFormsCore.UI.WebControls.TextBox>();
            ctrl3.ID = "tbPassword";
            ctrl1.AddParsedSubObject(ctrl3);

            
            this.tbPassword = ctrl3;
        #line hidden
        
        #line 14 "Example.aspx"
            ctrl1.AddParsedSubObject(WebActivator.CreateLiteral("<br />\n            "));
        #line hidden
        
        #line 15 "Example.aspx"
            var ctrl4 = WebActivator.CreateControl<global::WebFormsCore.UI.WebControls.Button>();
            ctrl4.ID = "btnLogin";
            ctrl1.AddParsedSubObject(ctrl4);

            
            ctrl4.Text = "Login";
            ((WebFormsCore.UI.IAttributeAccessor)ctrl4).SetAttribute("click", "btnLogin_Click");
            this.btnLogin = ctrl4;
        #line hidden
        
        #line 15 "Example.aspx"
            ctrl1.AddParsedSubObject(WebActivator.CreateLiteral("\n        </div>\n    "));
        #line hidden
        
        #line 17 "Example.aspx"
            this.AddParsedSubObject(WebActivator.CreateLiteral("\n</body>\n</html>"));
        #line hidden
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
        

        public override string AppRelativeVirtualPath => @"Example.ascx";

        public override string TemplateSourceDirectory => @"";

        protected override void FrameworkInitialize()
        {
            base.FrameworkInitialize();
            
        
        #line 1 "Example.ascx"
        #line hidden
        
        #line 1 "Example.ascx"
            this.AddParsedSubObject(WebActivator.CreateLiteral("\n\n"));
        #line hidden
        
        #line 3 "Example.ascx"
            var ctrl0 = WebActivator.CreateControl<global::WebFormsCore.UI.WebControls.Literal>();
            ctrl0.ID = "litTest";
            this.AddParsedSubObject(ctrl0);

            
            this.litTest = ctrl0;
        #line hidden
        
        #line 3 "Example.ascx"
            this.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
        #line hidden
        
        #line 4 "Example.ascx"
            var ctrl1 = WebActivator.CreateControl<global::WebFormsCore.UI.WebControls.Button>();
            ctrl1.ID = "btnIncrement";
            this.AddParsedSubObject(ctrl1);

            
            ctrl1.Click += (sender, e) =>
            {
                btnIncrement_OnClick(sender, e);
                return System.Threading.Tasks.Task.CompletedTask;
            };
            this.btnIncrement = ctrl1;
        #line hidden
        
        #line 4 "Example.ascx"
            ctrl1.AddParsedSubObject(WebActivator.CreateLiteral("Increment"));
        #line hidden
        }
    }
}
}