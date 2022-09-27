<%@ Control language="C#" Inherits="WebFormsCore.Example.Controls.TodoList" %>

<wfc:TextBox runat="server" ID="tbItem" class="form-control" OnEnterPressed="tbItem_OnEnterPressed" />

<wfc:Repeater runat="server" ID="rptItems" OnItemDataBound="rptItems_OnItemDataBound" ItemType="System.String">
    <ItemTemplate>
        <div class="mt-2">
            <wfc:Literal ID="litValue" runat="server" />
        </div>
    </ItemTemplate>
</wfc:Repeater>
