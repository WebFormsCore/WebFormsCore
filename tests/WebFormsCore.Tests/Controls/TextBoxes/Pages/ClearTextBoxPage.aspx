<%@ Page Language="C#" Inherits="WebFormsCore.Tests.Controls.TextBoxes.Pages.ClearTextBoxPage" %>
<%@ Register TagPrefix="asp" Namespace="WebFormsCore.UI.WebControls" Assembly="WebFormsCore" %>

<asp:TextBox runat="server" ID="textBox" />
<asp:TextBox runat="server" ID="txtMulti" TextMode="MultiLine" />
<asp:Button ID="btnPostback" runat="server" Text="Postback Only" />
<asp:Button ID="btnClear" runat="server" Text="Clear" OnClick="btnClear_Click" />
