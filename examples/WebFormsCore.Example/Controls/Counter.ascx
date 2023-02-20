<%@ Control language="C#" Inherits="WebFormsCore.Example.Controls.Counter" %>

<div class="mb-2">
    <% if (Count > 0) { %>
        <div class="alert alert-info p-2">
            🎉 Counter is greater than 0
        </div>
    <% } %>

    Counter: <wfc:Literal runat="server" ID="litCounter" />
</div>



<wfc:Button runat="server" ID="btnIncrement" Text="Increment" OnClick="btnIncrement_OnClick" class="btn btn-primary" />
