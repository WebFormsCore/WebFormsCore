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
    [WebFormsCore.CompiledView(@"Example.aspx", "C0CCEADD4ABA9918F5C5D9F4AFBBE6D4")]
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
            
            #line 8 "Example.aspx"
            await controls[0].RenderAsync(writer, token); // Text: "\n    "
            #line default
            
            #line 9 "Example.aspx"
            await writer.WriteObjectAsync( Test );
            #line default
            
            #line 9 "Example.aspx"
            await controls[1].RenderAsync(writer, token); // Text: "\n\n    "
            #line default
            
            #line 11 "Example.aspx"
            await controls[2].RenderAsync(writer, token); // Element: form
            #line default
            
            #line 17 "Example.aspx"
            await controls[3].RenderAsync(writer, token); // Text: "\n"
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
            var ctrl1 = WebActivator.CreateControl<global::WebFormsCore.UI.HtmlControls.HtmlHead>();
            ctrl0.AddParsedSubObject(ctrl1);

            
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
            // Unhandled type: Expression
        #line hidden
        
        #line 9 "Example.aspx"
            ctrl3.AddParsedSubObject(WebActivator.CreateLiteral("\n\n    "));
        #line hidden
        
        #line 11 "Example.aspx"
            var ctrl4 = WebActivator.CreateControl<global::WebFormsCore.UI.HtmlControls.HtmlForm>();
            ctrl4.ID = "form1";
            ctrl3.AddParsedSubObject(ctrl4);

            
            this.form1 = ctrl4;
        #line hidden
        
        #line 11 "Example.aspx"
            ctrl4.AddParsedSubObject(WebActivator.CreateLiteral("\n        "));
        #line hidden
        
        #line 12 "Example.aspx"
            var ctrl5 = WebActivator.CreateElement("div");
            ctrl4.AddParsedSubObject(ctrl5);
        #line hidden
        
        #line 12 "Example.aspx"
            ctrl5.AddParsedSubObject(WebActivator.CreateLiteral("\n            "));
        #line hidden
        
        #line 13 "Example.aspx"
            var ctrl6 = WebActivator.CreateControl<global::WebFormsCore.UI.WebControls.TextBox>();
            ctrl6.ID = "tbUsername";
            ctrl5.AddParsedSubObject(ctrl6);

            
            this.tbUsername = ctrl6;
        #line hidden
        
        #line 13 "Example.aspx"
            var ctrl7 = WebActivator.CreateElement("br");
            ctrl5.AddParsedSubObject(ctrl7);
        #line hidden
        
        #line 13 "Example.aspx"
            ctrl5.AddParsedSubObject(WebActivator.CreateLiteral("\n            "));
        #line hidden
        
        #line 14 "Example.aspx"
            var ctrl8 = WebActivator.CreateControl<global::WebFormsCore.UI.WebControls.TextBox>();
            ctrl8.ID = "tbPassword";
            ctrl5.AddParsedSubObject(ctrl8);

            
            this.tbPassword = ctrl8;
        #line hidden
        
        #line 14 "Example.aspx"
            var ctrl9 = WebActivator.CreateElement("br");
            ctrl5.AddParsedSubObject(ctrl9);
        #line hidden
        
        #line 14 "Example.aspx"
            ctrl5.AddParsedSubObject(WebActivator.CreateLiteral("\n            "));
        #line hidden
        
        #line 15 "Example.aspx"
            var ctrl10 = WebActivator.CreateControl<global::WebFormsCore.UI.WebControls.Button>();
            ctrl10.ID = "btnLogin";
            ctrl5.AddParsedSubObject(ctrl10);

            
            ctrl10.Text = "Login";
            ((WebFormsCore.UI.IAttributeAccessor)ctrl10).SetAttribute("click", "btnLogin_Click");
            this.btnLogin = ctrl10;
        #line hidden
        
        #line 15 "Example.aspx"
            ctrl5.AddParsedSubObject(WebActivator.CreateLiteral("\n        "));
        #line hidden
        
        #line 16 "Example.aspx"
            ctrl4.AddParsedSubObject(WebActivator.CreateLiteral("\n    "));
        #line hidden
        
        #line 17 "Example.aspx"
            ctrl3.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
        #line hidden
        
        #line 18 "Example.aspx"
            ctrl0.AddParsedSubObject(WebActivator.CreateLiteral("\n"));
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
            // Unhandled type: Directive
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