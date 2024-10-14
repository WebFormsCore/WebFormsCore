<%@ Page Inherits="WebFormsCore.Tests.Morphdom.Pages.UpdateScriptPage" %>
<%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" Assembly="WebFormsCore" %>
<!DOCTYPE html>
<html>
<head>
</head>
<body>
<wfc:LinkButton runat="server" ID="btnSetScript" Text="Set Script" />
<wfc:PlaceHolder runat="server" ID="phScript" Visible="False">
<script>window.success = 'true';</script>
</wfc:PlaceHolder>
</body>
</html>