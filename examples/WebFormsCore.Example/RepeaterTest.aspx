<%@ Page language="C#" Inherits="WebFormsCore.Example.RepeaterTest" %>

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8"/>
    <title runat="server" id="title"></title>
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.2.0/dist/css/bootstrap.min.css" integrity="sha384-gH2yIJqKdNHPEq0n4Mqa/HGKIhSkIHeL5AyhkYV8i59U5AR6csBvApHHNl/vI1Bx" crossorigin="anonymous">
</head>
<body>
    <wfc:Repeater runat="server" ID="list" OnNeedDataSource="OnNeedDataSource" ItemType="WebFormsCore.Example.TodoModel" DataKeys="Id" LoadDataOnPostBack="True">
        <ItemTemplate>
            <%# Item.Title %> <wfc:LinkButton runat="server" Text="Delete" OnClick="Delete_OnClick" />
        </ItemTemplate>
        <SeparatorTemplate>
            <hr />
        </SeparatorTemplate>
    </wfc:Repeater>
</body>
</html>
