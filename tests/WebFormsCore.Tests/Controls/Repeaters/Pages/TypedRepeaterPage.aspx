<%@ Page Inherits="WebFormsCore.Tests.Controls.Repeaters.Pages.TypedRepeaterPage" %>
<%@ Register TagPrefix="asp" Namespace="WebFormsCore.UI.WebControls" Assembly="WebFormsCore" %>

<ul>
<asp:Repeater runat="server" ID="items" OnItemDataBound="items_OnItemDataBound" DataKeys="Id" ItemType="WebFormsCore.Tests.Controls.Repeaters.Pages.RepeaterDataItem">
    <ItemTemplate>
        <li runat="server"><%# Item.Text %></li>
    </ItemTemplate>
</asp:Repeater>
</ul>