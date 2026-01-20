<%@ Page Language="C#" Inherits="WebFormsCore.Tests.Controls.Forms.Pages.NoForm" %>
<%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" Assembly="WebFormsCore" %>
<!DOCTYPE html>
<html lang="en">
<body>
<wfc:Label runat="server" ID="counter" Text="0" />
<wfc:Button runat="server" ID="button" Text="Increment" OnClick="IncrementCounter" />
</body>
</html>
