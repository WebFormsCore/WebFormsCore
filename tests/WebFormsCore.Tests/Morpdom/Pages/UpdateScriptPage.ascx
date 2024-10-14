<%@ Page Inherits="WebFormsCore.Tests.Morphdom.Pages.UpdateScriptPage" %>
<%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" Assembly="WebFormsCore" %>
<!DOCTYPE html>
<html>
<head>
</head>
<body>
<wfc:LinkButton runat="server" ID="btnSetScript" Text="Set Script" />
<wfc:PlaceHolder runat="server" ID="phScript" Visible="False">
<script>window.counter = (window.counter || 0) + 1;</script>
</wfc:PlaceHolder>
</body>
</html>