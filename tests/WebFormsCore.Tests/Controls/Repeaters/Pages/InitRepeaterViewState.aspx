<%@ Page Language="C#" Inherits="WebFormsCore.Tests.Controls.Repeaters.Pages.InitRepeaterViewState" %>
<%@ Register TagPrefix="asp" Namespace="WebFormsCore.UI.WebControls" Assembly="WebFormsCore" %>

<asp:Label runat="server" ID="lblViewState" />

<asp:Repeater runat="server" ID="rptItems" OnItemCreated="rptItems_OnItemCreated">
    <ItemTemplate>
        <asp:Label runat="server" ID="lblItem" CssClass="repeater-label" />
    </ItemTemplate>
</asp:Repeater>

<asp:LinkButton runat="server" ID="btnSubmit" Text="Submit" />