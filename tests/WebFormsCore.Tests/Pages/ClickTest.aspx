<%@ Page Inherits="WebFormsCore.Tests.Pages.ClickTest" %>
<%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" Assembly="WebFormsCore" %>
<!DOCTYPE html>
<html lang="en">
<body>
<wfc:Button runat="server" ID="btnSetResult" OnClick="btnSetResult_OnClick" Text="Click me" />
<wfc:Label runat="server" ID="lblResult"/>
</body>
</html>