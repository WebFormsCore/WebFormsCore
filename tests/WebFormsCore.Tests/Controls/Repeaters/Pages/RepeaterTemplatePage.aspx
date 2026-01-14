<%@ Page Language="C#" Inherits="WebFormsCore.Tests.Controls.Repeaters.Pages.RepeaterTemplatePage" %>
<%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" Assembly="WebFormsCore" %>

<wfc:Repeater ID="templateRepeater" runat="server" ItemType="System.String">
    <HeaderTemplate>
        <header>Header</header>
    </HeaderTemplate>
    <ItemTemplate>
        <div class="item"><%# Item %></div>
    </ItemTemplate>
    <SeparatorTemplate>
        <hr />
    </SeparatorTemplate>
    <FooterTemplate>
        <footer>Footer</footer>
    </FooterTemplate>
</wfc:Repeater>
