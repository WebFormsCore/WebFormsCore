<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="WebForms.NetFx.Example.Default" %>
<%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" %>

<!DOCTYPE html>
<html>
<head runat="server">
    <title></title>
</head>
<body>
    <form ID="Form" runat="server" method="post">
        <div>
            <wfc:Literal runat="server" ID="litText" /><br />
            <wfc:Button runat="server" ID="btnTest2">Test</wfc:Button>
        </div>
    </form>


<script type="module">
import morphdom from 'https://cdn.jsdelivr.net/npm/morphdom@2.6.1/dist/morphdom-esm.js';

const elements = document.querySelectorAll('[data-wf-form]');

for (const element of elements) {
    element.addEventListener('submit', function(e) {
        e.preventDefault();
        const form = e.target;
        const url = location.pathname + location.search;
        const data = new FormData(form);
        
        for (const pair of form) {
            data.append(pair[0], pair[1]);
        }
        
        fetch(url, {
            method: 'post',
            body: data,
        })
        .then(r => r.text())
        .then(r => {
            const newElements = [];

            const options = {
                onNodeAdded: function(node) {
                    newElements.push(node);
                }
            };

            if ('isEqualNode' in Element.prototype) {
                options.onBeforeElUpdated = (fromEl, toEl) => {
                    return !fromEl.isEqualNode(toEl);
                };
            }
            const parser = new DOMParser();
            const htmlDoc = parser.parseFromString(r, 'text/html');
            const newForm = htmlDoc.querySelector('form');

            morphdom(form, newForm);
        });
    });
}

</script>
</body>
</html>
