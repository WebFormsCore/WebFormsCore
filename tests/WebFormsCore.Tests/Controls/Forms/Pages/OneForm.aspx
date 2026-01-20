<%@ Page Language="C#" Inherits="WebFormsCore.Tests.Controls.Forms.Pages.OneForm" %>
<%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" Assembly="WebFormsCore" %>
<%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.HtmlControls" Assembly="WebFormsCore" %>
<!DOCTYPE html>
<html lang="en">
<body>
<wfc:HtmlForm runat="server" ID="form1">
    <wfc:Label runat="server" ID="counter" Text="0" />
    <wfc:Button runat="server" ID="button" Text="Increment" OnClick="IncrementCounter" />
</wfc:HtmlForm>
</body>
</html>
