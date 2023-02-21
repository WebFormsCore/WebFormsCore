import morphdom from "morphdom/dist/morphdom-esm";

function submitForm(form?: HTMLFormElement, eventTarget?: string) {
    const pageState = document.getElementById("pagestate") as HTMLInputElement;
    const url = location.pathname + location.search;

    const formData = form ? new FormData(form) : new FormData();

    // Add all the form elements that are not in a form
    const elements = document.body.querySelectorAll('input, select, textarea');

    for (let i = 0; i < elements.length; i++) {
        const element = elements[i] as HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement;

        if (element.hasAttribute('data-wfc-ignore') || element.type === "button" ||
                element.type === "submit" || element.type === "reset") {
            continue;
        }

        const form = element.closest('form[data-wfc-form]') as HTMLFormElement;

        if (form) {
            continue;
        }

        if (element.type === "checkbox" || element.type === "radio") {
            if ((element as HTMLInputElement).checked) {
                formData.append(element.name, element.value);
            }
        } else {
            formData.append(element.name, element.value);
        }
    }

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
    if (document.body.querySelector('input[type="file"]:not([data-wfc-ignore])')) {
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

                    if (node.tagName === 'DIV' && node.hasAttribute('data-wfc-owner') && (node.getAttribute('data-wfc-owner') ?? "") !== (form?.id ?? "")) {
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

        submitForm(form, eventTarget);
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

    e.preventDefault();
    submitForm(form, eventTarget);
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
    const eventTarget = e.target.getAttribute('name');
    e.preventDefault();
    submitForm(form, eventTarget);
});
