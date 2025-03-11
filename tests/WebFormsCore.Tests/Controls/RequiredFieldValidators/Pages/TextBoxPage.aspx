<%@ Page Language="C#" Inherits="WebFormsCore.Tests.Controls.RequiredFieldValidators.Pages.TextBoxPage" %>
<%@ Register TagPrefix="asp" Namespace="WebFormsCore.UI.WebControls" Assembly="WebFormsCore" %>

<asp:TextBox runat="server" ID="textBox" />
<asp:RequiredFieldValidator runat="server" ControlToValidate="textBox" Text="Required" ID="validator" />
<asp:Button ID="button" runat="server" Text="Postback" OnClick="button_OnClick" />
<asp:Label runat="server" ID="labelValue" />
<asp:Label runat="server" ID="labelPostback" />