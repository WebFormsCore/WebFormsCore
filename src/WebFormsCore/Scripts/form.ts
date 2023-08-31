import morphAttrs from "morphdom/src/morphAttrs";
import morphdomFactory from "morphdom/src/morphdom";
import { Mutex } from 'async-mutex';

const morphdom = morphdomFactory(morphAttrs);

const postbackMutex = new Mutex();

class ViewStateContainer {

    constructor(private element: HTMLElement | undefined, private formData: FormData) {
    }

    querySelector(selector: string) {
        if (this.element) {
             const result = this.element.querySelector(selector);

            if (result) {
                return result;
            }
        }

        return document.body.closest(":not([data-wfc-form]) " + selector);
    }

    querySelectorAll(selector: string) {
        const elements = document.body.querySelectorAll(":not([data-wfc-form]) " + selector);

        if (this.element) {
            return [
                ...this.element.querySelectorAll(selector),
                ...elements
            ];
        } else {
            return Array.from(elements);
        }
    }

    addInputs(selector: string) {
        const elements = this.querySelectorAll(selector);

        for (let i = 0; i < elements.length; i++) {
            const element = elements[i] as HTMLFormElement;

            addElement(element, this.formData);
        }
    }

}

function postBackElement(element: Element, eventTarget?: string, eventArgument?: string) {
    const form = getForm(element);
    const streamPanel = getStreamPanel(element);

    if (streamPanel) {
        return sendToStream(streamPanel, eventTarget, eventArgument);
    } else {
        return submitForm(form, eventTarget, eventArgument);
    }
}

function sendToStream(streamPanel: HTMLElement, eventTarget?: string, eventArgument?: string) {
    const webSocket = streamPanel.webSocket as WebSocket;

    if (!webSocket) {
        throw new Error("No WebSocket connection");
    }

    const data = {
        t: eventTarget,
        a: eventArgument
    };

    webSocket.send(JSON.stringify(data));
}

function addElement(element: HTMLFormElement, formData: FormData) {
    if (element.type === "checkbox" || element.type === "radio") {
        if (element.checked) {
            formData.append(element.name, element.value);
        }
    } else {
        formData.append(element.name, element.value);
    }
}

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

function hasElementFile(element: HTMLElement) {
    const elements = document.body.querySelectorAll('input[type="file"]');

    for (let i = 0; i < elements.length; i++) {
        const element = elements[i] as HTMLInputElement;

        if (element.files.length > 0) {
            return true;
        }
    }

    return false;
}

function getForm(element: Element) {
    return element.closest('[data-wfc-form]') as HTMLElement
}

function getStreamPanel(element: Element) {
    return element.closest('[data-wfc-stream]') as HTMLElement
}

function addInputs(formData: FormData, root: HTMLElement, addFormElements: boolean) {
    // Add all the form elements that are not in a form
    const elements = [];

    // @ts-ignore
    for (const element of root.querySelectorAll('input, select, textarea')) {
        if (!element.closest('[data-wfc-ignore]')) {
            elements.push(element);
        }
    }

    document.dispatchEvent(new CustomEvent("wfc:addInputs", {detail: {elements}}));

    for (let i = 0; i < elements.length; i++) {
        const element = elements[i] as HTMLFormElement;

        if (element.hasAttribute('data-wfc-ignore') || element.type === "button" ||
            element.type === "submit" || element.type === "reset") {
            continue;
        }

        if (element.closest('[data-wfc-ignore]')) {
            continue;
        }

        if (!addFormElements && getForm(element)) {
            continue;
        }

        if (getStreamPanel(element)) {
            continue;
        }

        addElement(element, formData);
    }
}

async function submitForm(form?: HTMLElement, eventTarget?: string, eventArgument?: string) {
    const release = await postbackMutex.acquire();

    try {
        const url = location.pathname + location.search;

        let formData: FormData

        if (form) {
            if (form.tagName === "FORM") {
                formData = new FormData(form as HTMLFormElement);
            } else {
                formData = new FormData()
                addInputs(formData, form, true);
            }
        } else {
            formData = new FormData();
        }

        addInputs(formData, document.body, false);

        if (eventTarget) {
            formData.append("wfcTarget", eventTarget);
        }

        if (eventArgument) {
            formData.append("wfcArgument", eventArgument);
        }

        const container = new ViewStateContainer(form, formData);
        document.dispatchEvent(new CustomEvent("wfc:beforeSubmit", {detail: {container, eventTarget}}));

        const request: RequestInit = {
            method: "POST"
        };

        request.body = hasElementFile(document.body) ? formData : new URLSearchParams(formData as any);

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
        const options = getMorpdomSettings(form);

        const parser = new DOMParser();
        const htmlDoc = parser.parseFromString(text, 'text/html');

        morphdom(document.head, htmlDoc.querySelector('head'), options);
        morphdom(document.body, htmlDoc.querySelector('body'), options);

        document.dispatchEvent(new CustomEvent("wfc:afterSubmit", {detail: {container, form, eventTarget}}));
    } finally {
        release();
    }
}

function getMorpdomSettings(form?: HTMLElement) {
    return {
        onNodeAdded(node) {
            document.dispatchEvent(new CustomEvent("wfc:addNode", {detail: {node, form}}));

            if (node.nodeType === Node.ELEMENT_NODE) {
                document.dispatchEvent(new CustomEvent("wfc:addElement", {detail: {element: node, form}}));
            }
        },
        onBeforeElUpdated: function(fromEl, toEl) {
            if (!fromEl.dispatchEvent(new CustomEvent("wfc:beforeUpdateNode", {cancelable: true, bubbles: true, detail: {node: fromEl, source: toEl, form}}))) {
                return false;
            }

            if (fromEl.nodeType === Node.ELEMENT_NODE && !fromEl.dispatchEvent(new CustomEvent("wfc:beforeUpdateElement", {cancelable: true, bubbles: true, detail: {element: fromEl, source: toEl, form}}))) {
                return false;
            }

            if (fromEl.hasAttribute('data-wfc-ignore') || toEl.hasAttribute('data-wfc-ignore')) {
                return false;
            }

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
        onElUpdated(el) {
            if (el.nodeType === Node.ELEMENT_NODE) {
                el.dispatchEvent(new CustomEvent("wfc:updateElement", { bubbles: true, detail: {element: el, form} }));
            }
        },
        onBeforeNodeDiscarded(node) {
            if (node.tagName === "SCRIPT" || node.tagName === "STYLE" || node.tagName === "LINK" && node.hasAttribute('rel') && node.getAttribute('rel') === 'stylesheet') {
                return false;
            }

            if (node.tagName === 'FORM' && node.hasAttribute('data-wfc-form')) {
                return false;
            }

            if (node.tagName === 'DIV' && node.hasAttribute('data-wfc-owner') && (node.getAttribute('data-wfc-owner') ?? "") !== (form?.id ?? "")) {
                return false;
            }

            if (!node.dispatchEvent(new CustomEvent("wfc:discardNode", { bubbles: true, cancelable: true, detail: { node, form } }))) {
                return false;
            }

            if (node.nodeType === Node.ELEMENT_NODE && !node.dispatchEvent(new CustomEvent("wfc:discardElement", { bubbles: true, cancelable: true, detail: { element: node, form } }))) {
                return false;
            }
        }
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

    e.preventDefault();

    postBackElement(e.target, eventTarget);
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

    const eventTarget = e.target.getAttribute('name');

    e.preventDefault();

    await postBackElement(e.target, eventTarget, 'ENTER');
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

    postBackChange(e.target);
});

function postBackChange(target: Element, timeOut = 1000, eventArgument: string = 'CHANGE') {
    const container = getStreamPanel(target) ?? getForm(target);
    const eventTarget = target.getAttribute('name');
    const key = (container?.id ?? '') + eventTarget + eventArgument;

    if (timeouts[key]) {
        clearTimeout(timeouts[key]);
    }

    timeouts[key] = setTimeout(async () => {
        delete timeouts[key];
        await postBackElement(target, eventTarget, eventArgument);
    }, timeOut);
}

function postBack(target: Element, eventArgument?: string) {
    const eventTarget = target.getAttribute('name');

    return postBackElement(target, eventTarget, eventArgument);
}

document.addEventListener('change', async function(e){
    if (e.target instanceof Element && e.target.hasAttribute('data-wfc-autopostback')) {
        const eventTarget = e.target.getAttribute('name');
        const container = getStreamPanel(e.target) ?? getForm(e.target);
        const key = (container?.id ?? '') + eventTarget;

        if (timeouts[key]) {
            clearTimeout(timeouts[key]);
        }

        setTimeout(() => postBackElement(e.target as Element, eventTarget, 'CHANGE'), 10);
    }
});

const WebFormsCore = {
    postBackChange,
    postBack,
    bind: function(selectors, options) {
        const init = options.init ?? function() {};
        const update = options.update ?? function() {};
        const submit = options.submit;
        const destroy = options.destroy;

        for (const element of document.querySelectorAll(selectors)) {
            init(element);
            update(element, element);
        }

        document.addEventListener('wfc:addElement', function (e: CustomEvent) {
            const { element } = e.detail;

            if (element.matches(selectors)) {
                init(element);
                update(element, element);
            }
        });

        document.addEventListener('wfc:beforeUpdateElement', function (e: CustomEvent) {
            const { element, source } = e.detail;

            if (element.matches(selectors) && update(element, source)) {
                e.preventDefault();
            }
        });

        if (submit) {
            document.addEventListener('wfc:beforeSubmit', function (e: CustomEvent) {
                const {container} = e.detail;

                for (const element of container.querySelectorAll(selectors)) {
                    submit(element, container.formData);
                }
            });
        }

        if (destroy) {
            document.addEventListener('wfc:discardElement', function (e: CustomEvent) {
                const {element} = e.detail;

                if (element.matches(selectors)) {
                    destroy(element);
                    e.preventDefault();
                }
            });
        }
    }
};

WebFormsCore.bind('[data-wfc-stream]', {
    init: function(element) {
        const id = element.id;
        let search = location.search;

        if (!search) {
            search = "?";
        } else {
            search += "&";
        }

        search += "__panel=" + id;

        const webSocket = new WebSocket(
            (location.protocol === "https:" ? "wss://" : "ws://") + location.host + location.pathname + search
        );

        element.webSocket = webSocket;
        element.isUpdating = false;

        webSocket.addEventListener('message', function(e) {
            const parser = new DOMParser();
            const htmlDoc = parser.parseFromString(`<!DOCTYPE html><html><body>${e.data}</body></html>`, 'text/html');

            element.isUpdating = true;
            morphdom(element, htmlDoc.getElementById(id), getMorpdomSettings());
            element.isUpdating = false;
        });
    },
    update: function(element, source) {
        if (!element.isUpdating) {
            return true;
        }
    },
    destroy: function(element) {
        const webSocket = element.webSocket as WebSocket;

        if (webSocket) {
            webSocket.close();
        }
    }
})

window.WebFormsCore = WebFormsCore;

declare global {
    interface Window {
        WebFormsCore: typeof WebFormsCore;
    }

    interface Element {
        webSocket: WebSocket | undefined;
    }
}