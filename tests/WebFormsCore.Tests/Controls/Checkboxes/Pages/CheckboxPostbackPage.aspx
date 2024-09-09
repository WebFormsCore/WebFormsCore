<%@ Page Language="C#" Inherits="WebFormsCore.Tests.Controls.Checkboxes.Pages.CheckboxPostbackPage" %>
<%@ Register TagPrefix="asp" Namespace="WebFormsCore.UI.WebControls" Assembly="WebFormsCore" %>

<asp:CheckBox ID="checkbox" runat="server" OnCheckedChanged="checkbox_OnCheckedChanged" />
<asp:Label runat="server" ID="label" Text="Unchanged" />
<asp:Button ID="button" runat="server" Text="Postback" />