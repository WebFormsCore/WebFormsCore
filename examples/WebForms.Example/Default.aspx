<%@ Page language="C#" Inherits="WebForms.Example.Default" %>
<%@ Register TagPrefix="app" Namespace="WebForms.Example.Controls" %>

<!DOCTYPE html>
<html lang="en">
<head id="Head" runat="server">
    <meta charset="UTF-8" />
    <title runat="server" id="title"></title>
</head>
<body id="Body" runat="server">

    <form runat="server" method="post">
        <app:Counter runat="server" />
    </form>

    <form runat="server" method="post">
        <app:Counter runat="server" />
    </form>

<script type="module">
import morphdom from 'https://cdn.jsdelivr.net/npm/morphdom@2.6.1/dist/morphdom-esm.js';

const elements = document.querySelectorAll('[data-wfc-form]');

for (const element of elements) {
    element.addEventListener('submit', function(e) {
        e.preventDefault();
        const form = e.target;
        const scope = form.getAttribute('data-wfc-form');
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
                onNodeAdded(node) {
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

            if (scope === 'global') {
                morphdom(document.head, htmlDoc.querySelector('head'));
                morphdom(document.body, htmlDoc.querySelector('body'));
            } else {
                morphdom(form, htmlDoc.querySelector('form'));
            }
        });
    });
}
</script>
</body>
</html>
