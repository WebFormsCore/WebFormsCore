<%@ Page Inherits="WebFormsCore.Tests.Pages.Repeater" %>
<%@ Register TagPrefix="asp" Namespace="WebFormsCore.UI.WebControls" Assembly="WebFormsCore" %>

<ul>
<asp:Repeater runat="server" ID="items" OnItemDataBound="items_OnItemDataBound" DataKeys="Id">
    <ItemTemplate>
        <li><asp:Literal runat="server" ID="litText" /></li>
    </ItemTemplate>
</asp:Repeater>
</ul>
