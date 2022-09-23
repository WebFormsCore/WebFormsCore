<%@ Control language="C#" Inherits="WebForms.Example.Controls.Counter" %>
<%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" %>

<div class="card">
    <div class="card-body">
        <div class="mb-2">
            <wfc:Literal runat="server" ID="litValue" />
        </div>
        <div class="mb-4">
            Prefix: <wfc:TextBox runat="server" ID="tbPrefix" value="test" AutoPostBack="True" class="form-control" />
        </div>
        <wfc:Button runat="server" ID="btnIncrement" class="btn btn-primary">Increment</wfc:Button>

        <hr />

        <ul>
            <wfc:Repeater runat="server" ItemType="System.String" ID="rptItems">
                <ItemTemplate>
                    <li>Test</li>
                </ItemTemplate>
            </wfc:Repeater>
        </ul>
    </div>
</div>
