<%@ Page language="C#" Inherits="WebFormsCore.Example.Default" Async="true" %>
<%@ Import Namespace="HttpStack" %>
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
                        <%
                            foreach (var kv in Context.Request.Query)
                            {
                                await Response.WriteAsync($"<div>{kv.Key} = {kv.Value}</div>");
                            }
                        %>
                    </div>
                </div>

            </div>
            <div class="col-6">
                <form runat="server" method="post" ID="formTodo">
                    <div class="card">
                        <div class="card-header">
                            <strong>Todo list</strong>
                        </div>
                        <div class="card-body">
                            <wfc:PlaceHolder runat="server" ID="phTodoContainer"/>
                        </div>
                    </div>
                </form>
            </div>
        </div>
    </div>
</div>

<script>
    document.addEventListener("wfc:submitError", function () {
        alert('Invalid viewstate');
    });
    </script>
</body>
</html>
