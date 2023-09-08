<%@ Page language="C#" Inherits="WebFormsCore.Example.Default" EnableViewState="False" EnableCsp="True" %>
<%@ Register TagPrefix="app" Namespace="WebFormsCore.Example" %>

<!DOCTYPE html>
<html lang="en">
<head id="Head" runat="server">
    <meta charset="UTF-8"/>
    <title runat="server" id="title"></title>
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.2.0/dist/css/bootstrap.min.css" integrity="sha384-gH2yIJqKdNHPEq0n4Mqa/HGKIhSkIHeL5AyhkYV8i59U5AR6csBvApHHNl/vI1Bx" crossorigin="anonymous">
</head>
<body id="Body" runat="server">
    <div class="container mt-4">
        <wfc:HtmlForm runat="server" EnableViewState="True" UpdatePage="False">
            <app:Counter runat="server" />
        </wfc:HtmlForm>

        <hr />

        <wfc:StreamPanel runat="server">
            <app:Counter runat="server" />
        </wfc:StreamPanel>

        <hr />

        <img src="https://placehold.co/600x400" />
    </div>
</body>
</html>
