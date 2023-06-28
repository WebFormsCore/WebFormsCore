<%@ Control language="C#" Inherits="WebFormsCore.Example.Controls.Counter" %>

<div class="mb-2">Counter: <wfc:Literal runat="server" ID="litCounter" /></div>
<wfc:Button runat="server" ID="btnIncrement" Text="Increment" OnClick="btnIncrement_OnClick" class="btn btn-primary" />

<% if (Count > 0) { %>
    <div class="alert alert-info p-2 mb-0 mt-4">
        🎉 Counter is greater than 0. It is <%= Count %>.
    </div>
<% } %>
