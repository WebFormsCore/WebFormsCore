<%@ Page Language="C#" MasterPageFile="Controls/MasterPages/Pages/TestSite.master" Inherits="WebFormsCore.Tests.Controls.MasterPages.Pages.MasterPageContentPage" %>
<%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" Assembly="WebFormsCore" %>
<%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.HtmlControls" Assembly="WebFormsCore" %>

<wfc:Content ContentPlaceHolderID="head" runat="server">
    <link rel="stylesheet" href="test.css" />
</wfc:Content>

<wfc:Content ContentPlaceHolderID="main" runat="server">
    <wfc:HtmlForm runat="server" ID="form1">
        <wfc:Label runat="server" ID="lblMessage" Text="Hello from content page" />
        <wfc:Button runat="server" ID="btnPostback" Text="Click Me" OnClick="btnPostback_OnClick" />
        <wfc:Label runat="server" ID="lblResult" Text="" />
    </wfc:HtmlForm>
</wfc:Content>
