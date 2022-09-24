<%@ Page language="C#" Inherits="WebForms.Example.Default" %>
<%@ Register TagPrefix="app" Namespace="WebForms.Example.Controls" %>

<!DOCTYPE html>
<html lang="en">
<head id="Head" runat="server">
    <meta charset="UTF-8" />
    <title runat="server" id="title"></title>
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.2.0/dist/css/bootstrap.min.css" integrity="sha384-gH2yIJqKdNHPEq0n4Mqa/HGKIhSkIHeL5AyhkYV8i59U5AR6csBvApHHNl/vI1Bx" crossorigin="anonymous">
</head>
<body id="Body" runat="server">

    <div class="container">
        <div class="mt-4">
            <form runat="server" method="post">
                <app:Counter runat="server" />
            </form>

            <form runat="server" method="post">
                <app:Counter runat="server" />
            </form>
        </div>
    </div>

<script type="module">
import morphdom from 'https://cdn.jsdelivr.net/npm/morphdom@2.6.1/dist/morphdom-esm.js';

const elements = document.querySelectorAll('[data-wfc-form]');

let eventTarget = null;

function submitForm(form) {
    const scope = form.getAttribute('data-wfc-form');
    const url = location.pathname + location.search;

    const data = new FormData(form);

    if (eventTarget) {
        data.append("__EVENTTARGET", eventTarget);
        eventTarget = null;
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
            },
            onBeforeElUpdated(fromEl, toEl) {
                if (fromEl.isEqualNode(toEl)) {
                    return false;
                }

                if (fromEl.tagName === 'SCRIPT') {
                    return false;
                }

                return true;
            }
        };

        const parser = new DOMParser();
        const htmlDoc = parser.parseFromString(r, 'text/html');

        if (scope === 'global') {
            morphdom(document.head, htmlDoc.querySelector('head'), options);
            morphdom(document.body, htmlDoc.querySelector('body'), options);
        } else {
            morphdom(form, htmlDoc.querySelector('form'), options);
        }
    });
}

const originalSubmit = HTMLFormElement.prototype.submit;

HTMLFormElement.prototype.submit = function() {
    if (this.getAttribute('data-wfc-form') !== null) {
        submitForm(this);
    } else {
        originalSubmit.call(this);
    }
};

document.addEventListener('submit', function(e){
    if(e.target && e.target.getAttribute('data-wfc-form') !== null) {
        e.preventDefault();
        submitForm(e.target);
    }
});

document.addEventListener('change', function(e){
    if(e.target && e.target.getAttribute('data-wfc-autopostback') !== null) {
        eventTarget = e.target.getAttribute('name');
        e.target.closest('form').submit();
    }
});

document.addEventListener('click', function(e){
    eventTarget = e.target?.closest("[data-wfc-postback]")?.getAttribute('data-wfc-postback');

    if (eventTarget) {
        e.preventDefault();
        e.target.closest('form')?.submit();
    }
});

document.addEventListener('keypress', function(e){
    if (e.key === 'Enter' || e.keyCode === 13 || e.which === 13) {
        e.preventDefault();
        eventTarget = e.target.getAttribute('name');
        e.target.closest('form').submit();
    }
});

</script>
</body>
</html>
