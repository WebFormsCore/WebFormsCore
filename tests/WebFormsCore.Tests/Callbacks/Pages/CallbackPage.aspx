<%@ Page Language="C#" Inherits="WebFormsCore.Tests.Callbacks.Pages.CallbackPage" %>
<%@ Register TagPrefix="asp" Namespace="WebFormsCore.UI.WebControls" Assembly="WebFormsCore" %>

<span data-wfc-ignore id="value"></span>
<asp:LinkButton ID="btnSetValue" runat="server" Text="Set Value" OnClick="btnSetValue_Click" CssClass="btn btn-primary" />
<script>
wfc.registerCallback('setValue', function (value) {
    document.getElementById('value').innerText = value;
});
</script>