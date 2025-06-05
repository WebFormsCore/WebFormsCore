<%@ Page Language="C#" Inherits="WebFormsCore.Tests.Controls.DropDown.Pages.DropDownPage" %>
<%@ Register TagPrefix="asp" Namespace="WebFormsCore.UI.WebControls" Assembly="WebFormsCore" %>

<asp:DropDownList runat="server" ID="ddl" AutoPostBack="True" OnSelectedIndexChanged="ddl_OnSelectedIndexChanged" />
<asp:Button runat="server" ID="btn" Text="Postback" />