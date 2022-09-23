<%@ Control language="C#" Inherits="WebForms.Example.Controls.Counter" %>
<%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" %>

<div>
    <wfc:Literal runat="server" ID="litValue" />
</div>
<wfc:Button runat="server">Increment</wfc:Button>
