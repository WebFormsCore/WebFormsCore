<%@ Page language="C#" Inherits="WebFormsCore.Tests.Controls.BackgroundControl.Pages.SlowPage" %>
<%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" Assembly="WebFormsCore" %>
<!DOCTYPE html>
<html lang="en">
<body>
<wfc:Button runat="server" ID="btnPostback" OnClick="btnPostback_Click">Postback</wfc:Button>
</body>
</html>