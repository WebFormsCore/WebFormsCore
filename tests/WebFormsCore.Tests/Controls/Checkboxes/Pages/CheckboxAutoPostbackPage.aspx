<%@ Page Language="C#" Inherits="WebFormsCore.Tests.Controls.Checkboxes.Pages.CheckboxAutoPostbackPage" %>
<%@ Register TagPrefix="asp" Namespace="WebFormsCore.UI.WebControls" Assembly="WebFormsCore" %>

<asp:CheckBox ID="checkbox" runat="server" AutoPostBack="True" OnCheckedChanged="checkbox_OnCheckedChanged" />
<asp:Label runat="server" ID="label" Text="Unchanged" />