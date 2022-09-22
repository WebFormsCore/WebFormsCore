<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="WebForms.NetFx.Example.Default" %>
<%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" %>

<!DOCTYPE html>
<html>
<head runat="server">
    <title></title>
</head>
<body>
    <form ID="Form" runat="server" method="post">
        <div>
            <wfc:Literal runat="server" ID="litText" /><br />
            <wfc:Button runat="server" ID="btnTest2">Test</wfc:Button>
        </div>
    </form>
</body>
</html>
