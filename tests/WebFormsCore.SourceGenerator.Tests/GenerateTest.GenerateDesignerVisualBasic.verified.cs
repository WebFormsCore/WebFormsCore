//HintName: WebForms.Designer.vb

Imports Microsoft.VisualBasic
<Assembly:WebFormsCore.RootNamespaceAttribute("Tests")>
<Assembly:WebFormsCore.AssemblyViewAttribute("DefaultPage.aspx", GetType(CompiledViews.DefaultPage_aspx))>
<WebFormsCore.ViewPath("DefaultPage.aspx")>
Partial Class DefaultPage
    Protected btnAdd As Global.WebFormsCore.UI.WebControls.Button
    Protected rptItems As Global.WebFormsCore.UI.WebControls.Repeater
    Protected litItem As Global.WebFormsCore.UI.WebControls.Literal
End Class

Public Partial Class CompiledViews
    

     <WebFormsCore.CompiledView("DefaultPage.aspx", "D2A9B3C5114485AA2FABFBA4F610123D")>
     Public Class DefaultPage_aspx
        Inherits DefaultPage

        Private Async Function Render_0(writer As WebFormsCore.UI.HtmlTextWriter, controls As WebFormsCore.UI.ControlCollection, token As System.Threading.CancellationToken) As System.Threading.Tasks.Task
            Await controls(0).RenderAsync(writer, token) ' Text: "\n                        "
             If True Then 
            Await controls(1).RenderAsync(writer, token) ' Text: "\n                            "
            Await controls(2).RenderAsync(writer, token) ' Element: Literal
            Await controls(3).RenderAsync(writer, token) ' Text: "\n                        "
             End If 
            Await controls(4).RenderAsync(writer, token) ' Text: "\n                    "
        End Function

        Public Overrides ReadOnly Property AppRelativeVirtualPath As String
            Get
                Return "DefaultPage.aspx"
            End Get
        End Property

        Public Overrides ReadOnly Property TemplateSourceDirectory As String
            Get
                Return ""
            End Get
        End Property

        Protected Overrides Sub FrameworkInitialize()
            MyBase.FrameworkInitialize()
            
            ' Unhandled type: Directive
            Me.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + ""))
            ' Unhandled type: Directive
            Me.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "" + vbLf + ""))
            Me.AddParsedSubObject(WebActivator.CreateLiteral("<!DOCTYPE html>"))
            Me.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + ""))
            Dim ctrl0 = WebActivator.CreateElement("html")
            Me.AddParsedSubObject(ctrl0)
            ctrl0.Attributes.Add("lang", "en")
            ctrl0.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + ""))
            Dim ctrl1 = WebActivator.CreateControl(Of Global.WebFormsCore.UI.HtmlControls.HtmlBody)()
            ctrl1.ID = "Body"
            ctrl0.AddParsedSubObject(ctrl1)
            ctrl1.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "" + vbLf + "    "))
            Dim ctrl2 = WebActivator.CreateElement("div")
            ctrl1.AddParsedSubObject(ctrl2)
            ctrl2.Attributes.Add("class", "container")
            ctrl2.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "        "))
            Dim ctrl3 = WebActivator.CreateElement("div")
            ctrl2.AddParsedSubObject(ctrl3)
            ctrl3.Attributes.Add("class", "mt-4")
            ctrl3.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "            "))
            Dim ctrl4 = WebActivator.CreateControl(Of Global.WebFormsCore.UI.HtmlControls.HtmlForm)()
            ctrl3.AddParsedSubObject(ctrl4)
            DirectCast(ctrl4, WebFormsCore.UI.IAttributeAccessor).SetAttribute("method", "post")
            ctrl4.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                "))
            Dim ctrl5 = WebActivator.CreateControl(Of Global.WebFormsCore.UI.WebControls.Button)()
            ctrl5.ID = "btnAdd"
            ctrl4.AddParsedSubObject(ctrl5)
            AddHandler ctrl5.Click, Function(sender, e)
                
                btnAdd_OnClick(sender, e)
                Return System.Threading.Tasks.Task.CompletedTask
            End function
            Me.btnAdd = ctrl5
            ctrl5.AddParsedSubObject(WebActivator.CreateLiteral("Add"))
            ctrl4.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                "))
            Dim ctrl6 = WebActivator.CreateControl(Of Global.WebFormsCore.UI.WebControls.Repeater)()
            ctrl6.ID = "rptItems"
            ctrl4.AddParsedSubObject(ctrl6)
            Me.rptItems = ctrl6
            ctrl6.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                    "))
            Dim ctrl7 = WebActivator.CreateElement("ItemTemplate")
            ctrl6.AddParsedSubObject(ctrl7)
            ctrl7.SetRenderMethodDelegate(AddressOf Render_0)
            ctrl7.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                        "))
            ' Unhandled type: Statement
            ctrl7.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                            "))
            Dim ctrl8 = WebActivator.CreateControl(Of Global.WebFormsCore.UI.WebControls.Literal)()
            ctrl8.ID = "litItem"
            ctrl7.AddParsedSubObject(ctrl8)
            Me.litItem = ctrl8
            ctrl7.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                        "))
            ' Unhandled type: Statement
            ctrl7.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                    "))
            ctrl6.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                "))
            ctrl4.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "            "))
            ctrl3.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "        "))
            ctrl2.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "    "))
            ctrl1.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "" + vbLf + ""))
            ctrl0.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + ""))
        End Sub
    End Class
End Class