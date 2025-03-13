<%@ Page language="C#" Inherits="WebFormsCore.Example.RepeaterTest" %>

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8"/>
    <title runat="server" id="title"></title>
</head>
<body>
    <wfc:Repeater runat="server" ID="empty">
        <NoDataTemplate>
            <div class="alert alert-info">No data</div>
        </NoDataTemplate>
    </wfc:Repeater>

    <wfc:Repeater runat="server" ID="list" OnNeedDataSource="OnNeedDataSource" ItemType="WebFormsCore.Example.TodoModel" DataKeys="Id" LoadDataOnPostBack="True" PageSize="5" PageIndex="2">
        <ItemTemplate>
            <%# Item.Title %> <wfc:LinkButton runat="server" Text="Delete" OnClick="Delete_OnClick" />
        </ItemTemplate>
        <SeparatorTemplate>
            <hr />
        </SeparatorTemplate>
    </wfc:Repeater>
</body>
</html>
<link rel="stylesheet" href="/bootstrap.min.css">