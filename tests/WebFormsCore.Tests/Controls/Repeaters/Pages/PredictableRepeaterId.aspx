<%@ Page Inherits="WebFormsCore.Tests.Controls.Repeaters.Pages.PredictableRepeaterId" %>
<%@ Register TagPrefix="asp" Namespace="WebFormsCore.UI.WebControls" Assembly="WebFormsCore" %>

<asp:Repeater runat="server" ID="items" OnItemDataBound="items_OnItemDataBound">
    <ItemTemplate>
        <asp:Label runat="server" ID="lbl" CssClass="result"></asp:Label>
    </ItemTemplate>
</asp:Repeater>
