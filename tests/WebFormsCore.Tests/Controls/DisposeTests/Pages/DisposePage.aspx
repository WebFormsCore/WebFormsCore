<%@ Page language="C#" Inherits="WebFormsCore.Tests.Controls.DisposeTests.Pages.DisposePage" %>
<%@ Register TagPrefix="app" Namespace="WebFormsCore.Tests.Controls.DisposeTests.Pages" %>
<!DOCTYPE html>
<html lang="en">
<body>
<app:DisposableControl runat="server" ID="staticControl" />
<app:DynamicControl runat="server" />
</body>
</html>
