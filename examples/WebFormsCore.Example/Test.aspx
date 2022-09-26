<%@ Page language="C#" Inherits="WebFormsCore.Example.Default" %>
<%@ Register TagPrefix="app" Namespace="WebFormsCore.Example.Controls" %>

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
                <app:Counter runat="server" />
            </form>

            <form runat="server" method="post">
                <app:Counter runat="server" />
            </form>
        </div>
    </div>

</body>
</html>
