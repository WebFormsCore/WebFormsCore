<%@ Page Inherits="WebFormsCore.Tests.Pages.LargeViewStateTest" Language="C#" %>
<%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" Assembly="WebFormsCore" %>
<!DOCTYPE html>
<html lang="en">
<body>
<wfc:Repeater runat="server" ID="rptItems" OnItemDataBound="rptItems_OnItemDataBound" OnNeedDataSource="rptItems_OnNeedDataSource" DataKeys="Id" LoadDataOnPostBack="True">
    <ItemTemplate>
        <div runat="server" ID="container">
            <wfc:Label runat="server" ID="lblName" />
            <wfc:LinkButton runat="server" ID="btnSetId" OnClick="btnSetId_OnClick" CssClass="btn" Text="Trigger" />
        </div>
    </ItemTemplate>
</wfc:Repeater>
</body>
</html>