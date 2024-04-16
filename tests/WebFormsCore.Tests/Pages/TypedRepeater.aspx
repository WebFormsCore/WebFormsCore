<%@ Page Inherits="WebFormsCore.Tests.Pages.TypedRepeater" %>
<%@ Register TagPrefix="asp" Namespace="WebFormsCore.UI.WebControls" Assembly="WebFormsCore" %>

<ul>
<asp:Repeater runat="server" ID="items" OnItemDataBound="items_OnItemDataBound">
    <ItemTemplate>
        <li><asp:Literal runat="server" ID="item" /></li>
    </ItemTemplate>
</asp:Repeater>
</ul>