<%@ Page Language="C#" Route="/" CodeBehind="Default.aspx.cs" Inherits="WebFormsCore.Web.Default" %>

<!DOCTYPE html>
<html>
<head>
    <title>WebFormsCore.Web</title>
</head>
<body>
    <form id="form1" runat="server">
        <h1>Hello WebForms Core!</h1>
        <wfc:Literal ID="litMessage" runat="server" />
        <br />
        <wfc:Button ID="btnClick" runat="server" Text="Click Me" OnClick="btnClick_Click" />
    </form>
</body>
</html>
