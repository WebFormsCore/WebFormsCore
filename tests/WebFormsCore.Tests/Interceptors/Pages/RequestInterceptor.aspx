<%@ Page Inherits="WebFormsCore.Tests.Interceptors.Pages.RequestInterceptor" %>
<%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" Assembly="WebFormsCore" %>

<wfc:Label runat="server" ID="lblHeaderXTest" />
<wfc:LinkButton runat="server" ID="btnSubmit" Text="Submit" />

<script>
document.addEventListener("wfc:beforeSubmit", function (e) {
    e.detail.addRequestInterceptor(async function (request) {
        // Simulate a delay
        await new Promise(resolve => setTimeout(resolve, 50));

        // Add a custom header
        request.headers["X-Test"] = "Success";
    });
});
</script>