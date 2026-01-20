<%@ Page Language="C#" Inherits="WebFormsCore.Tests.Controls.Pages.LoadOrder" %>
<%@ Register TagPrefix="asp" Namespace="WebFormsCore.UI.WebControls" Assembly="WebFormsCore" %>
<%@ Register TagPrefix="test" TagName="LoadOrderControl" Src="LoadOrderControl.ascx" %>

<test:LoadOrderControl runat="server" Text="Success" ID="loadControl" />