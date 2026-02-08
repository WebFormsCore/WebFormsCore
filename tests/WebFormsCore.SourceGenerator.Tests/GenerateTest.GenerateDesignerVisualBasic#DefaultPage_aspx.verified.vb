//HintName: DefaultPage_aspx.vb



Imports Microsoft.VisualBasic
<Assembly:WebFormsCore.RootNamespaceAttribute("Tests")>
<Assembly:WebFormsCore.AssemblyViewAttribute("DefaultPage.aspx", GetType(CompiledViews_Tests.DefaultPage_aspx))>
<WebFormsCore.ViewPath("DefaultPage.aspx")>
Partial Class DefaultPage
    Protected btnAdd As Global.WebFormsCore.UI.WebControls.Button
    Protected rptItems As Global.WebFormsCore.UI.WebControls.Repeater
    Protected litItem As Global.WebFormsCore.UI.WebControls.Literal
End Class

Public Partial Class CompiledViews_Tests
    

     <WebFormsCore.CompiledView("DefaultPage.aspx", "D2A9B3C5114485AA2FABFBA4F610123D")>
     Public Class DefaultPage_aspx
        Inherits DefaultPage
	Protected Shadows Body As Global.WebFormsCore.UI.HtmlControls.HtmlBody

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
            
            Me.AddParsedSubObject(WebActivator.CreateLiteral("<!DOCTYPE html>"))
            Me.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + ""))
            Dim ctrl0 = WebActivator.CreateElement("html")
            Me.AddParsedSubObject(ctrl0)
            ctrl0.Attributes.Add("lang", "en")
            ctrl0.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + ""))
            Dim ctrl1 = WebActivator.CreateControl(Of Global.WebFormsCore.UI.HtmlControls.HtmlBody)()
            ctrl1.ID = "Body"
            ctrl0.AddParsedSubObject(ctrl1)

            
            Me.Body = ctrl1
            ctrl1.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "" + vbLf + "    <div class=""container"">" + vbLf + "        <div class=""mt-4"">" + vbLf + "            "))
            Dim ctrl2 = WebActivator.CreateControl(Of Global.WebFormsCore.UI.HtmlControls.HtmlForm)()
            ctrl1.AddParsedSubObject(ctrl2)

            
            ctrl2.Method = WebActivator.ParseAttribute(Of String)("post")
            ctrl2.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                "))
            Dim ctrl3 = WebActivator.CreateControl(Of Global.WebFormsCore.UI.WebControls.Button)()
            ctrl3.ID = "btnAdd"
            ctrl2.AddParsedSubObject(ctrl3)

            
            AddHandler ctrl3.Click, Function(sender, e)
                btnAdd_OnClick(sender, e)
                Return System.Threading.Tasks.Task.CompletedTask
            End function
            Me.btnAdd = ctrl3
            ctrl3.AddParsedSubObject(WebActivator.CreateLiteral("Add"))
            ctrl2.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                "))
            Dim ctrl4 = WebActivator.CreateControl(Of Global.WebFormsCore.UI.WebControls.Repeater)()
            ctrl4.ID = "rptItems"
            ctrl2.AddParsedSubObject(ctrl4)

            
            Me.rptItems = ctrl4
            ctrl4.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                    "))
            Dim ctrl5 = WebActivator.CreateElement("ItemTemplate")
            ctrl4.AddParsedSubObject(ctrl5)
            ctrl5.SetRenderMethodDelegate(AddressOf Render_0)
            ctrl5.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                        "))
            ' Unhandled type: Statement
            ctrl5.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                            "))
            Dim ctrl6 = WebActivator.CreateControl(Of Global.WebFormsCore.UI.WebControls.Literal)()
            ctrl6.ID = "litItem"
            ctrl5.AddParsedSubObject(ctrl6)

            
            Me.litItem = ctrl6
            ctrl5.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                        "))
            ' Unhandled type: Statement
            ctrl5.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                    "))
            ctrl4.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                "))
            ctrl2.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "            "))
            ctrl1.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "        </div>" + vbLf + "    </div>" + vbLf + "" + vbLf + ""))
            ctrl0.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + ""))
        End Sub
    End Class
End Class
