<%@ Page Language="C#" Inherits="WebFormsCore.Tests.Controls.CustomValidators.Pages.TextBoxPage" %>
<%@ Register TagPrefix="asp" Namespace="WebFormsCore.UI.WebControls" Assembly="WebFormsCore" %>

<asp:TextBox runat="server" ID="textBox" />
<asp:CustomValidator runat="server" ControlToValidate="textBox" OnServerValidate="validator_OnServerValidate" Text="Invalid" ID="validator" />
<asp:Button ID="button" runat="server" Text="Postback" OnClick="button_OnClick" />
<asp:Label runat="server" ID="labelValue" />
<asp:Label runat="server" ID="labelPostback" />