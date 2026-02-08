<%@ Page Language="C#" MasterPageFile="Controls/MasterPages/Pages/TestSite.master" Inherits="WebFormsCore.Tests.Controls.MasterPages.Pages.FindControlPage" %>
<%@ MasterType TypeName="WebFormsCore.Tests.Controls.MasterPages.Pages.TestSite" %>
<%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" Assembly="WebFormsCore" %>
<%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.HtmlControls" Assembly="WebFormsCore" %>

<wfc:Content ContentPlaceHolderID="main" runat="server">
    <wfc:HtmlForm runat="server" ID="form1">
        <wfc:Label runat="server" ID="lblInContent" Text="InContent" />
    </wfc:HtmlForm>
</wfc:Content>
