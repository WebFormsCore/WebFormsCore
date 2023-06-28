//HintName: WebForms.Designer.vb

Imports Microsoft.VisualBasic
<Assembly:WebFormsCore.RootNamespaceAttribute("Tests")>
<Assembly:WebFormsCore.AssemblyViewAttribute("DefaultPage.aspx", GetType(CompiledViews.DefaultPage_aspx))>
<WebFormsCore.ViewPath("DefaultPage.aspx")>
Partial Class DefaultPage
    Protected Body As Global.WebFormsCore.UI.HtmlControls.HtmlBody
    Protected btnAdd As Global.WebFormsCore.UI.WebControls.Button
    Protected rptItems As Global.WebFormsCore.UI.WebControls.Repeater
End Class

Public Partial Class CompiledViews
    

     <WebFormsCore.CompiledView("DefaultPage.aspx", "D2A9B3C5114485AA2FABFBA4F610123D")>
     Public Class DefaultPage_aspx
        Inherits DefaultPage

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
            
            #ExternalSource("DefaultPage.aspx", 1)
            ' Unhandled type: Directive
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 1)
            Me.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + ""))
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 2)
            ' Unhandled type: Directive
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 2)
            Me.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "" + vbLf + ""))
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 4)
            Me.AddParsedSubObject(WebActivator.CreateLiteral("<!DOCTYPE html>"))
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 4)
            Me.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + ""))
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 5)
            Dim ctrl0 = WebActivator.CreateElement("html")
            Me.AddParsedSubObject(ctrl0)
            ctrl0.Attributes.Add("lang", "en")
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 5)
            ctrl0.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + ""))
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 6)
            Dim ctrl1 = WebActivator.CreateControl(Of Global.WebFormsCore.UI.HtmlControls.HtmlBody)()
            ctrl0.AddParsedSubObject(ctrl1)
            ctrl1.ID = WebActivator.ParseAttribute(Of String)("Body")
            Me.Body = ctrl1
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 6)
            ctrl1.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "" + vbLf + "    "))
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 8)
            Dim ctrl2 = WebActivator.CreateElement("div")
            ctrl1.AddParsedSubObject(ctrl2)
            ctrl2.Attributes.Add("class", "container")
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 8)
            ctrl2.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "        "))
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 9)
            Dim ctrl3 = WebActivator.CreateElement("div")
            ctrl2.AddParsedSubObject(ctrl3)
            ctrl3.Attributes.Add("class", "mt-4")
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 9)
            ctrl3.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "            "))
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 10)
            Dim ctrl4 = WebActivator.CreateControl(Of Global.WebFormsCore.UI.HtmlControls.HtmlForm)()
            ctrl3.AddParsedSubObject(ctrl4)
            DirectCast(ctrl4, WebFormsCore.UI.IAttributeAccessor).SetAttribute("method", "post")
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 10)
            ctrl4.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                "))
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 11)
            Dim ctrl5 = WebActivator.CreateControl(Of Global.WebFormsCore.UI.WebControls.Button)()
            ctrl4.AddParsedSubObject(ctrl5)
            AddHandler ctrl5.Click, Function(sender, e)
                
                btnAdd_OnClick(sender, e)
                Return System.Threading.Tasks.Task.CompletedTask
            End function
            ctrl5.ID = WebActivator.ParseAttribute(Of String)("btnAdd")
            Me.btnAdd = ctrl5
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 11)
            ctrl5.AddParsedSubObject(WebActivator.CreateLiteral("Add"))
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 11)
            ctrl4.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                "))
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 12)
            Dim ctrl6 = WebActivator.CreateControl(Of Global.WebFormsCore.UI.WebControls.Repeater)()
            ctrl4.AddParsedSubObject(ctrl6)
            ctrl6.ID = WebActivator.ParseAttribute(Of String)("rptItems")
            ctrl6.ItemTemplate = new Template_DefaultPage_ctrl6_ItemTemplate(WebActivator, Me)
            Me.rptItems = ctrl6
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 12)
            ctrl6.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                    "))
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 17)
            ctrl6.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                "))
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 18)
            ctrl4.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "            "))
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 19)
            ctrl3.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "        "))
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 20)
            ctrl2.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "    "))
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 21)
            ctrl1.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "" + vbLf + ""))
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 23)
            ctrl0.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + ""))
            #End ExternalSource
        End Sub
        Public Class Template_DefaultPage_ctrl6_ItemTemplate
            Implements WebFormsCore.UI.ITemplate

	        Private ReadOnly _parent As DefaultPage_aspx

            Public Sub New(webActivator As WebFormsCore.IWebObjectActivator, parent As DefaultPage_aspx)
                Me.WebActivator = webActivator
		        Me._parent = parent
            End Sub

            Public WebActivator As WebFormsCore.IWebObjectActivator

        Private Async Function Render_0(writer As WebFormsCore.UI.HtmlTextWriter, controls As WebFormsCore.UI.ControlCollection, token As System.Threading.CancellationToken) As System.Threading.Tasks.Task
            #ExternalSource("DefaultPage.aspx", 13)
            Await controls(0).RenderAsync(writer, token) ' Text: "\n                        "
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 14)
             If True Then 
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 14)
            Await controls(1).RenderAsync(writer, token) ' Text: "\n                            "
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 15)
            Await controls(2).RenderAsync(writer, token) ' Element: Literal
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 15)
            Await controls(3).RenderAsync(writer, token) ' Text: "\n                        "
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 16)
             End If 
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 16)
            Await controls(4).RenderAsync(writer, token) ' Text: "\n                    "
            #End ExternalSource
        End Function

            Public Sub InstantiateIn(container As WebFormsCore.UI.Control) Implements WebFormsCore.UI.ITemplate.InstantiateIn
                
            #ExternalSource("DefaultPage.aspx", 13)
            container.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                        "))
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 14)
            ' Unhandled type: Statement
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 14)
            container.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                            "))
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 15)
            Dim ctrl8 = WebActivator.CreateControl(Of Global.WebFormsCore.UI.WebControls.Literal)()
            container.AddParsedSubObject(ctrl8)
            ctrl8.ID = WebActivator.ParseAttribute(Of String)("litItem")
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 15)
            container.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                        "))
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 16)
            ' Unhandled type: Statement
            #End ExternalSource
            #ExternalSource("DefaultPage.aspx", 16)
            container.AddParsedSubObject(WebActivator.CreateLiteral("" + vbLf + "                    "))
            #End ExternalSource
            End Sub
        End Class
    End Class
End Class