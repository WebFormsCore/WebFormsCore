import morphAttrs from "morphdom/src/morphAttrs";
import morphdomFactory from "morphdom/src/morphdom";
import { Mutex } from 'async-mutex';

const morphdom = morphdomFactory(morphAttrs);

const postbackMutex = new Mutex();

function syncBooleanAttrProp(fromEl, toEl, name) {
    if (fromEl[name] !== toEl[name]) {
        fromEl[name] = toEl[name];
        if (fromEl[name]) {
            fromEl.setAttribute(name, '');
        } else {
            fromEl.removeAttribute(name);
        }
    }
}

async function submitForm(form?: HTMLFormElement, eventTarget?: string, eventArgument?: string) {
    const release = await postbackMutex.acquire();
    try {
        const pageState = document.getElementById("pagestate") as HTMLInputElement;
        const url = location.pathname + location.search;

        const formData = form ? new FormData(form) : new FormData();

        if (pageState) {
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

            formData.append("__PAGESTATE", pageState.value);
        }

        if (eventTarget) {
            formData.append("__EVENTTARGET", eventTarget);
        }

        if (eventArgument) {
            formData.append("__EVENTARGUMENT", eventArgument);
        }

        document.dispatchEvent(new CustomEvent("wfc:beforeSubmit", {detail: {form, eventTarget, formData}}));

        const request: RequestInit = {
            method: "POST",
        };

        // Determine if we need to send the form data as JSON or as form data
        if (document.body.querySelector('input[type="file"]:not([data-wfc-ignore])')) {
            request.body = formData;
        } else {
            const object = {};
            formData.forEach(function (value, key) {
                object[key] = value;
            });
            request.body = JSON.stringify(object);
            request.headers = {
                "Content-Type": "application/json",
            };
        }

        const response = await fetch(url, request)

        if (!response.ok) {
            document.dispatchEvent(new CustomEvent("wfc:submitError", {
                detail: {
                    form,
                    eventTarget,
                    response: response
                }
            }));
            throw new Error(response.statusText);
        }

        const text = await response.text();
        const newElements = [];

        const options = {
            onNodeAdded(node) {
                newElements.push(node);
                document.dispatchEvent(new CustomEvent("wfc:addNode", {detail: {node, form, eventTarget}}));
            },
            onBeforeElUpdated: function(fromEl, toEl) {
                if (fromEl.tagName === "INPUT" && fromEl.type !== "hidden") {
                    morphAttrs(fromEl, toEl);
                    syncBooleanAttrProp(fromEl, toEl, 'checked');
                    syncBooleanAttrProp(fromEl, toEl, 'disabled');

                    // Only update the value if the value attribute is present
                    if (toEl.hasAttribute('value')) {
                        fromEl.value = toEl.value;
                    }

                    return false;
                }
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

                const result = document.dispatchEvent(new CustomEvent("wfc:discardNode", {
                    detail: {
                        node,
                        form,
                        eventTarget
                    }, cancelable: true
                }));

                if (!result) {
                    return false;
                }
            }
        };

        const parser = new DOMParser();
        const htmlDoc = parser.parseFromString(text, 'text/html');

        morphdom(document.head, htmlDoc.querySelector('head'), options);
        morphdom(document.body, htmlDoc.querySelector('body'), options);

        document.dispatchEvent(new CustomEvent("wfc:afterSubmit", {detail: {form, eventTarget, newElements}}));
    } finally {
        release();
    }
}

const originalSubmit = HTMLFormElement.prototype.submit;

HTMLFormElement.prototype.submit = async function() {
    if (this.hasAttribute('data-wfc-form')) {
        await submitForm(this);
    } else {
        originalSubmit.call(this);
    }
};

document.addEventListener('submit', async function(e){
    if (e.target instanceof Element && e.target.hasAttribute('data-wfc-form')) {
        e.preventDefault();
        await submitForm(e.target as HTMLFormElement);
    }
});

document.addEventListener('click', async function(e){
    if (!(e.target instanceof Element)) {
        return;
    }

    const eventTarget = e.target?.closest("[data-wfc-postback]")?.getAttribute('data-wfc-postback');

    if (!eventTarget) {
        return;
    }

    const form = e.target.closest('form[data-wfc-form]') as HTMLFormElement;

    e.preventDefault();
    await submitForm(form, eventTarget);
});

document.addEventListener('keypress', async function(e){
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
    await submitForm(form, eventTarget, 'ENTER');
});

const timeouts = {};

document.addEventListener('input', function(e){
    if (!(e.target instanceof Element) || e.target.tagName !== "INPUT" || !e.target.hasAttribute('data-wfc-autopostback')) {
        return;
    }

    const type = e.target.getAttribute('type');

    if (type === "button" || type === "submit" || type === "reset") {
        return;
    }

    const form = e.target.closest('form[data-wfc-form]') as HTMLFormElement;
    const eventTarget = e.target.getAttribute('name');
    const key = (form?.id ?? '') + eventTarget;

    if (timeouts[key]) {
        clearTimeout(timeouts[key]);
    }

    timeouts[key] = setTimeout(async () => {
        delete timeouts[key];
        await submitForm(form, eventTarget, 'CHANGE');
    }, 1000);
});

document.addEventListener('change', async function(e){
    if(e.target instanceof Element && e.target.hasAttribute('data-wfc-autopostback')) {
        const eventTarget = e.target.getAttribute('name');
        const form = e.target.closest('form[data-wfc-form]') as HTMLFormElement;
        const key = (form?.id ?? '') + eventTarget;

        if (timeouts[key]) {
            clearTimeout(timeouts[key]);
        }

        setTimeout(() => submitForm(form, eventTarget, 'CHANGE'), 10);
    }
});
