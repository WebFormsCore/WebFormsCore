<%@ Page Language="C#" Inherits="WebFormsCore.Tests.Controls.Forms.Pages.TwoForms" %>
<%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" Assembly="WebFormsCore" %>
<%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.HtmlControls" Assembly="WebFormsCore" %>
<!DOCTYPE html>
<html lang="en">
<body>
<wfc:HtmlForm runat="server" ID="form1">
    <wfc:Label runat="server" ID="counter1" Text="0" />
    <wfc:Button runat="server" ID="button1" Text="Increment" OnClick="IncrementCounter1" />
</wfc:HtmlForm>

<wfc:HtmlForm runat="server" ID="form2">
    <wfc:Label runat="server" ID="counter2" Text="0" />
    <wfc:Button runat="server" ID="button2" Text="Increment" OnClick="IncrementCounter2" />
</wfc:HtmlForm>
</body>
</html>
