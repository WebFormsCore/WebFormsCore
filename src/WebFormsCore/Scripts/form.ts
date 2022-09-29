import morphdom from "morphdom/dist/morphdom-esm";

function submitForm(form: HTMLFormElement, eventTarget?: string) {
    const pageState = document.getElementById("pagestate") as HTMLInputElement;
    const url = location.pathname + location.search;

    const formData = new FormData(form);

    if (pageState) {
        formData.append("__PAGESTATE", pageState.value);
    }

    if (eventTarget) {
        formData.append("__EVENTTARGET", eventTarget);
        eventTarget = null;
    }

    const request: RequestInit = {
        method: "POST",
    };

    // Determine if we need to send the form data as JSON or as form data
    if (document.body.querySelector('input[type="file"]')) {
        request.body = formData;
    } else {
        const object = {};
        formData.forEach(function(value, key){
            object[key] = value;
        });
        request.body = JSON.stringify(object);
        request.headers = {
            "Content-Type": "application/json",
        };
    }

    fetch(url, request)
        .then(r => r.text())
        .then(r => {
            const newElements = [];

            const options = {
                onNodeAdded(node) {
                    newElements.push(node);
                },
                onBeforeNodeDiscarded(node) {
                    if (node.tagName === "SCRIPT") {
                        return false;
                    }

                    if (node.tagName === 'FORM' && node.hasAttribute('data-wfc-form')) {
                        return false;
                    }
                }
            };

            const parser = new DOMParser();
            const htmlDoc = parser.parseFromString(r, 'text/html');

            morphdom(document.head, htmlDoc.querySelector('head'), options);
            morphdom(document.body, htmlDoc.querySelector('body'), options);
        });
}

const originalSubmit = HTMLFormElement.prototype.submit;

HTMLFormElement.prototype.submit = function() {
    if (this.hasAttribute('data-wfc-form')) {
        submitForm(this);
    } else {
        originalSubmit.call(this);
    }
};

document.addEventListener('submit', function(e){
    if (e.target instanceof Element && e.target.hasAttribute('data-wfc-form')) {
        e.preventDefault();
        submitForm(e.target as HTMLFormElement);
    }
});

document.addEventListener('change', function(e){
    if(e.target instanceof Element && e.target.hasAttribute('data-wfc-autopostback')) {
        const eventTarget = e.target.getAttribute('name');
        const form = e.target.closest('form[data-wfc-form]') as HTMLFormElement;

        if (form) {
            submitForm(form, eventTarget);
        }
    }
});

document.addEventListener('click', function(e){
    if (!(e.target instanceof Element)) {
        return;
    }

    const eventTarget = e.target?.closest("[data-wfc-postback]")?.getAttribute('data-wfc-postback');

    if (!eventTarget) {
        return;
    }

    const form = e.target.closest('form[data-wfc-form]') as HTMLFormElement;
    if (form) {
        e.preventDefault();
        submitForm(form, eventTarget);
    }
});

document.addEventListener('keypress', function(e){
    if (e.key !== 'Enter' && e.keyCode !== 13 && e.which !== 13) {
        return;
    }

    if (!(e.target instanceof Element) || e.target.tagName !== "INPUT") {
        return;
    }

    const type = e.target.getAttribute('type');

    if (type === "button" || type === "submit" || type === "reset") {
        return;
    }

    const form = e.target.closest('form[data-wfc-form]') as HTMLFormElement;

    if (!form) {
        return;
    }

    const eventTarget = e.target.getAttribute('name');
    e.preventDefault();
    submitForm(form, eventTarget);
});
