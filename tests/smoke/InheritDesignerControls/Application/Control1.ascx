<%@ Control Language="C#" Inherits="Library.BaseControl" %>
<%@ Register TagPrefix="app" TagName="Test" Src="~/Test.ascx" %>
<%@ Register TagPrefix="library" TagName="LibraryControl" Src="~/LibraryControl.ascx" %>

<app:Test runat="server" ID="Test" />

<library:LibraryControl runat="server" ID="LibraryControl" />
