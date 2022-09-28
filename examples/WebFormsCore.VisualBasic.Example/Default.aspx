<%@ Page language="VB" Inherits="WebFormsCore.VisualBasic.Example.DefaultPage" %>
<%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" %>

<!DOCTYPE html>
<html lang="en">
<head id="Head" runat="server">
    <meta charset="UTF-8" />
    <title runat="server" id="title"></title>
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.2.0/dist/css/bootstrap.min.css" integrity="sha384-gH2yIJqKdNHPEq0n4Mqa/HGKIhSkIHeL5AyhkYV8i59U5AR6csBvApHHNl/vI1Bx" crossorigin="anonymous">
</head>
<body id="Body" runat="server">

    <div class="container">
        <div class="mt-4">
            <form runat="server" method="post">
                <wfc:Literal runat="server" ID="litValue" Text="0" />

                <div>
                    <% If Counter > 0 Then %>
                        <div>We're greater than 0!</div>
                    <% End If %>
                </div>

                <wfc:Button runat="server" ID="btnIncrement" OnClick="btnIncrement_OnClick" Text="Increment" />

                <wfc:Repeater runat="server" ID="rptItems" OnItemDataBound="rptItems_OnItemDataBound">
                    <ItemTemplate>
                        <wfc:Literal runat="server" ID="litItem" />
                    </ItemTemplate>
                </wfc:Repeater>
            </form>
        </div>
    </div>
</body>
</html>
