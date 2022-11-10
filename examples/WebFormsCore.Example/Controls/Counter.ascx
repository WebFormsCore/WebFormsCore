<%@ Control language="C#" Inherits="WebFormsCore.Example.Controls.Counter" %>

<div class="mb-2">
    Counter: <wfc:Literal runat="server" ID="litCounter" />
</div>

<wfc:Button runat="server" ID="btnIncrement" Text="Increment" OnClick="btnIncrement_OnClick" class="btn btn-primary" />
