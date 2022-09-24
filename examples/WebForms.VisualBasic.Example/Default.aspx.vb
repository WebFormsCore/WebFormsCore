Imports WebFormsCore

Public Partial Class DefaultPage
    Inherits UI.Page

    Private Property Counter As Integer
        Get
            Return CInt(ViewState("Counter"))
        End Get
        Set
            ViewState("Counter") = Value
        End Set
    End Property

    Protected Function btnIncrement_OnClick(sender As Object, e As EventArgs)
        Counter += 1
    End Function

    Protected Overrides Sub OnPreRender(args As EventArgs)
        litValue.Text = Counter.ToString()
    End Sub
End Class
