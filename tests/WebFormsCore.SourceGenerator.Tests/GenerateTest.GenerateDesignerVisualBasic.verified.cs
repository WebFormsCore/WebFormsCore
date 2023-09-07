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
            Me.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "<html lang=""en"">" + vbLf + ""))
            Dim ctrl0 = WebActivator.CreateControl(Of Global.WebFormsCore.UI.HtmlControls.HtmlBody)()
            ctrl0.ID = "Body"
            Me.AddParsedSubObject(ctrl0)
            ctrl0.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "" + vbLf + "    <div class=""container"">" + vbLf + "        <div class=""mt-4"">" + vbLf + "            "))
            Dim ctrl1 = WebActivator.CreateControl(Of Global.WebFormsCore.UI.HtmlControls.HtmlForm)()
            ctrl0.AddParsedSubObject(ctrl1)
            DirectCast(ctrl1, WebFormsCore.UI.IAttributeAccessor).SetAttribute("method", "post")
            ctrl1.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                "))
            Dim ctrl2 = WebActivator.CreateControl(Of Global.WebFormsCore.UI.WebControls.Button)()
            ctrl2.ID = "btnAdd"
            ctrl1.AddParsedSubObject(ctrl2)
            AddHandler ctrl2.Click, Function(sender, e)
                
                btnAdd_OnClick(sender, e)
                Return System.Threading.Tasks.Task.CompletedTask
            End function
            Me.btnAdd = ctrl2
            ctrl2.AddParsedSubObject(WebActivator.CreateLiteral("Add"))
            ctrl1.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                "))
            Dim ctrl3 = WebActivator.CreateControl(Of Global.WebFormsCore.UI.WebControls.Repeater)()
            ctrl3.ID = "rptItems"
            ctrl1.AddParsedSubObject(ctrl3)
            Me.rptItems = ctrl3
            ctrl3.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                    "))
            Dim ctrl4 = WebActivator.CreateElement("ItemTemplate")
            ctrl3.AddParsedSubObject(ctrl4)
            ctrl4.SetRenderMethodDelegate(AddressOf Render_0)
            ctrl4.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                        "))
            ' Unhandled type: Statement
            ctrl4.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                            "))
            Dim ctrl5 = WebActivator.CreateControl(Of Global.WebFormsCore.UI.WebControls.Literal)()
            ctrl5.ID = "litItem"
            ctrl4.AddParsedSubObject(ctrl5)
            Me.litItem = ctrl5
            ctrl4.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                        "))
            ' Unhandled type: Statement
            ctrl4.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                    "))
            ctrl3.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                "))
            ctrl1.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "            "))
            ctrl0.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "        </div>" + vbLf + "    </div>" + vbLf + "" + vbLf + ""))
            Me.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "</html>"))
        End Sub
    End Class
End Class