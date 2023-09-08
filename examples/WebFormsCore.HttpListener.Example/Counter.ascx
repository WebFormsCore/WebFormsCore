<%@ Control Language="C#" Inherits="WebFormsCore.Example.Counter" %>

<div class="mb-2">
    Counter: <%= Value %>
</div>

<wfc:Button runat="server" ID="increment" OnClick="increment_OnClick" CssClass="btn btn-primary" Text="Increment" />