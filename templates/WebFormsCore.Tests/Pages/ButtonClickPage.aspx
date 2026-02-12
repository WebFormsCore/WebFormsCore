<%@ Page Language="C#" Inherits="WebFormsCore.Tests1.Pages.ButtonClickPage" %>

<!DOCTYPE html>
<html lang="en">
<body>
    <form id="form1" runat="server">
        <wfc:Button runat="server" ID="btnClick" OnClick="btnClick_OnClick" Text="Click me" />
        <wfc:Label runat="server" ID="lblResult" />
    </form>
</body>
</html>
