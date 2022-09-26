<%@ Control language="C#" Inherits="WebFormsCore.Example.Controls.Counter" %>
<%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" %>

<div class="card">
    <div class="card-body">
        <div class="mb-2">
            <wfc:Literal runat="server" ID="litValue" />
        </div>
        <div class="mb-4">
            Counter prefix: <wfc:TextBox runat="server" ID="tbPrefix" value="test" AutoPostBack="True" class="form-control" />
        </div>
        <wfc:Button runat="server" ID="btnIncrement" class="btn btn-primary" OnClick="btnIncrement_OnClick">Increment</wfc:Button>

        <hr />

        <ul>
            <wfc:Repeater runat="server" ItemType="System.String" ID="rptItems" OnItemDataBound="rptItems_OnItemDataBound">
                <SeparatorTemplate>
                    <li>----</li>
                </SeparatorTemplate>
                <HeaderTemplate>
                    <li>Header</li>
                </HeaderTemplate>
                <FooterTemplate>
                    <li>Footer</li>
                </FooterTemplate>
                <ItemTemplate>
                    <li><wfc:Literal runat="server" ID="litItem" /></li>
                </ItemTemplate>
            </wfc:Repeater>
        </ul>
    </div>
</div>
