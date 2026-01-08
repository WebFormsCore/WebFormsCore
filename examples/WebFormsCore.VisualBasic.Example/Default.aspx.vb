Imports WebFormsCore.UI
Imports WebFormsCore.UI.WebControls

Public Partial Class DefaultPage
    Inherits UI.Page

    Protected Property Counter As Integer
        Get
            Return CInt(ViewState("Counter"))
        End Get
        Set
            ViewState("Counter") = Value
        End Set
    End Property

    Protected Async Function btnIncrement_OnClick(sender As Object, e As EventArgs) As Task
        Counter += 1

        rptItems.DataSource = Enumerable.Range(0, Counter)
        Await rptItems.DataBindAsync()
    End Function

    Protected Overrides Function OnPreRenderAsync(token As System.Threading.CancellationToken) As ValueTask
        litValue.Text = Counter.ToString()
        Return MyBase.OnPreRenderAsync(token)
    End Function

    Protected Function rptItems_OnItemDataBound(sender As Object, e As RepeaterItemEventArgs) As Task
        Dim item = e.Item.FindControls(Of ItemControls)()
        item.litItem.Text = $"Item {e.Item.ItemIndex}"
        Return Task.CompletedTask
    End Function
End Class
