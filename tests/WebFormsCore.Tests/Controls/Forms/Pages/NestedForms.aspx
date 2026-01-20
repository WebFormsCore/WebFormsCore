<%@ Page Language="C#" Inherits="WebFormsCore.Tests.Controls.Forms.Pages.NestedForms" %>
<%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" Assembly="WebFormsCore" %>
<%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.HtmlControls" Assembly="WebFormsCore" %>
<!DOCTYPE html>
<html lang="en">
<body>
<wfc:HtmlForm runat="server" ID="outerForm">
    <wfc:Label runat="server" ID="outerCounter" Text="0" />
    <wfc:Button runat="server" ID="outerButton" Text="Increment Outer" OnClick="IncrementOuterCounter" />

    <wfc:HtmlForm runat="server" ID="innerForm">
        <wfc:Label runat="server" ID="innerCounter" Text="0" />
        <wfc:Button runat="server" ID="innerButton" Text="Increment Inner" OnClick="IncrementInnerCounter" />
    </wfc:HtmlForm>
</wfc:HtmlForm>
</body>
</html>
