<%@ Page language="C#" Inherits="WebFormsCore.Example.Default" %>
<%@ Register TagPrefix="app" Namespace="WebFormsCore.Example.Controls" %>

<!DOCTYPE html>
<html lang="en">
<head id="Head" runat="server">
    <meta charset="UTF-8"/>
    <title runat="server" id="title"></title>
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.2.0/dist/css/bootstrap.min.css" integrity="sha384-gH2yIJqKdNHPEq0n4Mqa/HGKIhSkIHeL5AyhkYV8i59U5AR6csBvApHHNl/vI1Bx" crossorigin="anonymous">
</head>
<body id="Body" runat="server">

<div class="container">
    <div class="my-2">
        <wfc:CheckBox runat="server" ID="cb" OnCheckedChanged="cb_OnCheckedChanged" AutoPostBack="True" />
        <wfc:Literal runat="server" ID="litCb" />
    </div>

    <wfc:Button runat="server" Text="Download file" OnClick="btnDownload_OnClick" CssClass="btn btn-primary" />
    <wfc:Button runat="server" Text="Go to example" OnClick="btnRedirect_OnClick" CssClass="btn btn-primary" />

    <wfc:Panel runat="server">
    <div class="mt-4">
        <div class="row">
            <div class="col-6">
                <form runat="server" method="post" ID="formCounter">
                    <div class="card">
                        <div class="card-header">
                            <strong>Counter</strong>
                        </div>
                        <div class="card-body">
                            <app:Counter runat="server"/>
                        </div>
                    </div>
                </form>

                <div class="mt-4 card">
                    <div class="card-header">
                        <strong>Request Query</strong>
                    </div>
                    <div class="card-body">
                        <% foreach (var kv in Context.Request.Query) { %>
                            <%: kv.Key %> = <%: kv.Value %><br />
                        <% } %>
                    </div>
                </div>

            </div>
            <div class="col-6">
                <form runat="server" method="post" ID="formTodo">
                    <div class="mt-4 card">
                        <div class="card-header">
                            <strong>Todo list</strong>
                        </div>
                        <div class="card-body">
                            <wfc:PlaceHolder runat="server" ID="phTodoContainer" />
                        </div>
                    </div>
                </form>
            </div>
        </div>
    </div>
    </wfc:Panel>
</div>

<script>
    document.addEventListener("wfc:submitError", function () {
        alert('Invalid viewstate');
    });
    </script>
</body>
</html>
