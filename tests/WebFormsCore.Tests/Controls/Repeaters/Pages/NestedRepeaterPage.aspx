<%@ Page Language="C#" Inherits="WebFormsCore.Tests.Controls.Repeaters.Pages.NestedRepeaterPage" %>
<%@ Register TagPrefix="asp" Namespace="WebFormsCore.UI.WebControls" Assembly="WebFormsCore" %>

<asp:Repeater runat="server" ID="a" OnItemCreated="a_OnItemCreated">
    <ItemTemplate>
        <asp:Repeater runat="server" ID="b" OnItemCreated="b_OnItemCreated">
            <ItemTemplate>
                <asp:Label class="repeater-label" runat="server" ID="lbl" />
            </ItemTemplate>
        </asp:Repeater>
    </ItemTemplate>
</asp:Repeater>