<%@ Control language="C#" Inherits="WebFormsCore.Example.Controls.Clock" %>
<%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" Assembly="WebFormsCore" %>

<wfc:StreamPanel Prerender="True" runat="server">
    <wfc:Literal runat="server" ID="litTime" />
</wfc:StreamPanel>
