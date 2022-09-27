import morphdom from "morphdom/dist/morphdom-esm";

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
    if(e.target instanceof Element && e.target.getAttribute('data-wfc-form') !== null) {
        e.preventDefault();
        submitForm(e.target);
    }
});

document.addEventListener('change', function(e){
    if(e.target instanceof Element && e.target.getAttribute('data-wfc-autopostback') !== null) {
        eventTarget = e.target.getAttribute('name');
        e.target.closest('form').submit();
    }
});

document.addEventListener('click', function(e){
    if (!(e.target instanceof Element)) {
        return;
    }

    eventTarget = e.target?.closest("[data-wfc-postback]")?.getAttribute('data-wfc-postback');

    if (eventTarget) {
        e.preventDefault();
        e.target.closest('form')?.submit();
    }
});

document.addEventListener('keypress', function(e){
    if (e.target instanceof Element && (e.key === 'Enter' || e.keyCode === 13 || e.which === 13)) {
        e.preventDefault();
        eventTarget = e.target.getAttribute('name');
        e.target.closest('form').submit();
    }
});
