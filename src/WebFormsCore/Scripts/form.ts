import {PostBackOptions, WebFormsCore, WfcBeforeSubmitEvent, WfcValidateEvent} from "../../../typings";
import morphAttrs from "morphdom/src/morphAttrs";
import morphdomFactory from "morphdom/src/morphdom";
import DOMPurify from 'dompurify';
import { Mutex } from 'async-mutex';

const callbackDefinitions: { [name: string]: ((arg: any) => void | Promise<void>) } = {};
const pendingCallbacks: { [name: string]: any[] } = {};

const sanitise = (input: string, options: JavaScriptOptions) => {
    const allowedTags = [];

    if (options.updateScripts) {
        allowedTags.push('script');
    }

    if (options.updateStyles) {
        allowedTags.push('style');
    }

    return DOMPurify.sanitize(input, {
        RETURN_TRUSTED_TYPE: true,
        WHOLE_DOCUMENT: true,
        ADD_TAGS: allowedTags,
    })
}

const morphdom = morphdomFactory((fromEl, toEl) => {
    if (!fromEl.dispatchEvent(new CustomEvent("wfc:beforeUpdateAttributes", {cancelable: true, bubbles: true, detail: {node: fromEl, source: toEl}}))) {
        return;
    }

    morphAttrs(fromEl, toEl);

    if (!fromEl.dispatchEvent(new CustomEvent("wfc:updateAttributes", {bubbles: true, detail: {node: fromEl, source: toEl}}))) {
        return;
    }
});

const postbackMutex = new Mutex();

const formMutexes = new WeakMap<HTMLElement, Mutex>();

function getFormMutex(form: HTMLElement): Mutex {
    let mutex = formMutexes.get(form);
    if (!mutex) {
        mutex = new Mutex();
        formMutexes.set(form, mutex);
    }
    return mutex;
}

function isScopedForm(form: HTMLElement | null | undefined): boolean {
    return form?.getAttribute('data-wfc-form') === 'self';
}

function getScopedAncestors(form: HTMLElement): HTMLElement[] {
    const ancestors: HTMLElement[] = [];
    let parent = form.parentElement?.closest('[data-wfc-form="self"]') as HTMLElement | null;
    while (parent) {
        ancestors.unshift(parent); // outermost first
        parent = parent.parentElement?.closest('[data-wfc-form="self"]') as HTMLElement | null;
    }
    return ancestors;
}

let pendingPostbacks = 0;

class ViewStateContainer {

    constructor(public element: HTMLElement | undefined, public formData: FormData) {
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

async function postBackElement(element: Element, eventTarget?: string, eventArgument?: string, options?: PostBackOptions) {
    if (!eventTarget) {
        eventTarget = element.getAttribute('data-wfc-postback') ?? "";
    }

    if ((options?.validate ?? true) && !await wfc.validate(element)) {
        return;
    }

    element.dispatchEvent(new CustomEvent("wfc:postbackTriggered"));

    try {
        const streamPanel = getStreamPanel(element);

        if (streamPanel) {
            await sendToStream(streamPanel, eventTarget, eventArgument);
        } else {
            // Check if we're inside a scoped form
            const scopedForm = getScopedForm(element);

            if (scopedForm) {
                await submitForm(element, scopedForm, eventTarget, eventArgument);
            } else {
                const form = getRootForm(element);
                await submitForm(element, form, eventTarget, eventArgument);
            }
        }
    } finally {
        element.dispatchEvent(new CustomEvent("wfc:afterPostbackTriggered"));
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

    return Promise.resolve();
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

function getScopedForm(element: Element): HTMLElement | null {
    return element.closest('[data-wfc-form="self"]') as HTMLElement | null;
}

function getRootForm(element: Element): HTMLElement | null {
    let form = element.closest('[data-wfc-form]') as HTMLElement | null;
    
    if (!form) {
        return null;
    }
    
    while (form && form.tagName !== 'FORM') {
        const parent = form.parentElement?.closest('[data-wfc-form]') as HTMLElement | null;
        if (!parent) {
            break;
        }
        form = parent;
    }
    
    return form;
}

function getStreamPanel(element: Element) {
    return element.closest('[data-wfc-stream]') as HTMLElement
}

function addInputs(formData: FormData, root: HTMLElement, addFormElements: boolean, allowFileUploads, skipNestedScoped: boolean = false) {
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

        // When collecting for a scoped form, skip inputs inside nested scoped forms
        if (skipNestedScoped) {
            const closestScoped = element.closest('[data-wfc-form="self"]') as HTMLElement | null;
            if (closestScoped && closestScoped !== root) {
                continue;
            }
        }

        if (getStreamPanel(element)) {
            continue;
        }

        if (!allowFileUploads && element.type === "file") {
            continue;
        }

        addElement(element, formData);
    }
}

function getFormData(form?: HTMLElement, eventTarget?: string, eventArgument?: string, allowFileUploads: boolean = true) {
    let formData: FormData
    const scoped = isScopedForm(form);

    if (form) {
        if (form.tagName === "FORM" && allowFileUploads && !scoped) {
            formData = new FormData(form as HTMLFormElement);
        } else {
            formData = new FormData()
            addInputs(formData, form, true, allowFileUploads, scoped);
        }
    } else {
        formData = new FormData();
    }

    addInputs(formData, document.body, false, allowFileUploads, false);

    if (eventTarget) {
        formData.append("wfcTarget", eventTarget);
    }

    if (eventArgument) {
        formData.append("wfcArgument", eventArgument);
    }

    return formData;
}

interface JavaScriptOptions {
    updateScripts: boolean
    updateStyles: boolean
}

function getOptions(data: string): JavaScriptOptions {
    return {
        updateScripts: data[0] === '1',
        updateStyles: data[1] === '1'
    }
}

async function submitForm(element: Element, form?: HTMLElement, eventTarget?: string, eventArgument?: string) {
    const baseElement = element.closest('[data-wfc-base]') as HTMLElement;
    let target: HTMLElement;

    if (baseElement) {
        target = baseElement;
    } else {
        target = document.body;
    }

    const scoped = isScopedForm(form);
    const url = baseElement?.getAttribute('data-wfc-base') ?? location.toString();
    const formData = getFormData(form, eventTarget, eventArgument);
    const container = new ViewStateContainer(form, formData);

    // For scoped forms, add the scoped indicator
    if (scoped) {
        formData.append("wfcScoped", "true");
    }

    pendingPostbacks++;

    // For scoped forms, acquire per-form mutex (and ancestor mutexes for nested scoped forms).
    // For non-scoped forms, use the global mutex.
    const releases: Array<() => void> = [];

    if (scoped && form) {
        // Acquire ancestor scoped form mutexes first (outermost to innermost)
        const ancestors = getScopedAncestors(form);
        for (const ancestor of ancestors) {
            releases.push(await getFormMutex(ancestor).acquire());
        }
        // Acquire this form's own mutex
        releases.push(await getFormMutex(form).acquire());
    } else {
        releases.push(await postbackMutex.acquire());
    }

    const interceptors: Array<(request: RequestInit) => void | Promise<void>> = [];

    try {
        const cancelled = !target.dispatchEvent(new CustomEvent("wfc:beforeSubmit", {
            bubbles: true,
            cancelable: true,
            detail: {
                target,
                container,
                eventTarget,
                element,
                addRequestInterceptor(input) {
                    interceptors.push(input);
                }
            } as WfcBeforeSubmitEvent
        }));

        if (cancelled) {
            return;
        }

        const request: RequestInit = {
            method: "POST",
            redirect: "error",
            credentials: "include",
            headers: {
                'X-IsPostback': 'true',
            }
        };

        request.body = hasElementFile(document.body) ? formData : new URLSearchParams(formData as any);

        for (const interceptor of interceptors) {
            const result = interceptor(request);

            if (result instanceof Promise) {
                await result;
            }
        }

        let response: Response;

        try {
            response = await fetch(url, request);
        } catch(e) {
            target.dispatchEvent(new CustomEvent("wfc:submitError", {
                bubbles: true,
                detail: {
                    form,
                    eventTarget,
                    response: undefined,
                    error: e
                }
            }));
            throw e;
        }

        if (!response.ok) {
            target.dispatchEvent(new CustomEvent("wfc:submitError", {
                bubbles: true,
                detail: {
                    form,
                    eventTarget,
                    response: response,
                    error: undefined
                }
            }));
            throw new Error(response.statusText);
        }

        const redirectTo = response.headers.get('x-redirect-to');

        if (redirectTo) {
            window.location.assign(redirectTo);
            return;
        }

        const contentDisposition = response.headers.get('content-disposition');

        if (response.status === 204) {
            // No Content
        } else if (response.ok && contentDisposition && contentDisposition.indexOf('attachment') !== -1) {
            // noinspection ES6MissingAwait
            receiveFile(element, response, contentDisposition);
        } else {
            const text = await response.text();

            const jsOptions: JavaScriptOptions = response.headers.has('x-wfc-options')
                ? getOptions(response.headers.get('x-wfc-options'))
                : {
                    updateScripts: false,
                    updateStyles: false
                };
            const options = getMorpdomSettings(jsOptions, form);

            const parser = new DOMParser();
            const htmlDoc = parser.parseFromString(sanitise(text, jsOptions), 'text/html');

            if (form && form.getAttribute('data-wfc-form') === 'self') {
                morphdom(form, htmlDoc.querySelector('[data-wfc-form]'), options);
            } else if (baseElement) {
                morphdom(baseElement, htmlDoc.querySelector('[data-wfc-base]'), options);
            } else {
                morphdom(document.head, htmlDoc.querySelector('head'), options);
                morphdom(document.body, htmlDoc.querySelector('body'), options);
            }
        }
    } finally {
        pendingPostbacks--;
        // Release all mutexes in reverse order (innermost first)
        for (let i = releases.length - 1; i >= 0; i--) {
            releases[i]();
        }
        target.dispatchEvent(new CustomEvent("wfc:afterSubmit", {bubbles: true, detail: {target, container, form, eventTarget}}));

        // Update the validators
        const validationGroups = new Set<string>();

        validationGroups.add("");

        for (const element of document.querySelectorAll('[data-wfc-validate]')) {
            const validationGroup = element.getAttribute('data-wfc-validate') ?? "";

            if (validationGroup) {
                validationGroups.add(validationGroup);
            }
        }

        for (const validationGroup of validationGroups) {
            await wfc.validate(validationGroup, true);
        }
    }
}

async function receiveFile(element: Element, response: Response, contentDisposition: string) {
    document.dispatchEvent(new CustomEvent("wfc:beforeFileDownload", {detail: {element, response}}));

    try {
        const contentEncoding = response.headers.get('content-encoding');
        const contentLength = response.headers.get(contentEncoding ? 'x-file-size' : 'content-length');

        if (contentLength) {
            const total = parseInt(contentLength,10);
            let loaded = 0;

            const reader = response.body.getReader();

            let cancelRequested = false;
            let onProgress = function(loaded: number, total: number) {
                const percent = Math.round(loaded / total * 100);
                document.dispatchEvent(new CustomEvent("wfc:progressFileDownload", {detail: {element, response, loaded, total, percent}}));
            }

            response = new Response(
                new ReadableStream({
                    start(controller) {
                        if (cancelRequested) {
                            controller.close();
                            return;
                        }

                        read();

                        function read() {
                            reader.read().then(({done, value}) => {
                                if (done) {
                                    // ensure onProgress called when content-length=0
                                    if (total === 0) {
                                        onProgress(loaded, total);
                                    }

                                    controller.close();
                                    return;
                                }

                                loaded += value.byteLength;
                                onProgress(loaded, total);
                                controller.enqueue(value);
                                read();
                            }).catch(error => {
                                console.error(error);
                                controller.error(error)
                            });
                        }
                    }
                })
            )
        }

        const fileNameMatch = contentDisposition.match(/filename=(?:"([^"]+)"|([^;]+))/);
        const blob = await response.blob();
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.style.display = 'none';

        if (fileNameMatch) {
            a.download = fileNameMatch[1] ?? fileNameMatch[2];
        } else {
            a.download = "download";
        }

        document.body.appendChild(a);
        a.click();

        setTimeout(() => {
            document.body.removeChild(a);
            URL.revokeObjectURL(url);
        }, 0);
    } finally {
        document.dispatchEvent(new CustomEvent("wfc:afterFileDownload", {detail: {element, response}}));
    }
}

function getMorpdomSettings(options: { updateScripts: boolean, updateStyles: boolean }, form?: HTMLElement) {
    return {
        getNodeKey(node) {
            if (node) {
                if (node.nodeName === 'SCRIPT' && (node.src || node.innerHTML)) {
                    return node.src || node.innerHTML;
                }

                if (node.nodeName === 'TEMPLATE' && node.innerHTML) {
                    return node.innerHTML;
                }

                if (node.nodeName === 'STYLE' && node.innerHTML) {
                    return node.innerHTML;
                }

                if (node.nodeName === 'LINK' && node.href) {
                    return node.href;
                }

                return (node.getAttribute && node.getAttribute('id')) || node.id;
            }
        },
        onBeforeNodeAdded: function(node) {
            if (node.nodeName === 'TEMPLATE' && node.hasAttribute('data-wfc-callbacks')) {
                handleCallbacks(node);
                return false;
            }

            return node;
        },
        onNodeAdded(node) {
            document.dispatchEvent(new CustomEvent("wfc:addNode", {detail: {node, form}}));

            if (node.nodeType === Node.ELEMENT_NODE) {
                document.dispatchEvent(new CustomEvent("wfc:addElement", {detail: {element: node, form}}));
            }

            if (node.nodeName === 'SCRIPT') {
                const script = document.createElement('script');

                for (let i = 0; i < node.attributes.length; i++) {
                    const attr = node.attributes[i];
                    script.setAttribute(attr.name, attr.value);
                }

                script.innerHTML = node.innerHTML;
                node.replaceWith(script);
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
                const hasValue = toEl.hasAttribute('value');

                // If the 'value' attribute is not set, set it to the current value
                if (!hasValue && fromEl.hasAttribute('value')) {
                    toEl.setAttribute('value', fromEl.getAttribute('value') ?? "");
                }

                morphAttrs(fromEl, toEl);

                if (hasValue && fromEl.value !== (toEl as HTMLInputElement).value) {
                    fromEl.value = (toEl as HTMLInputElement).value;
                }

                syncBooleanAttrProp(fromEl, toEl, 'checked');
                syncBooleanAttrProp(fromEl, toEl, 'disabled');

                return false;
            }

            if (fromEl.tagName === "TEXTAREA") {
                morphAttrs(fromEl, toEl);

                if (fromEl.value !== (toEl as HTMLTextAreaElement).value) {
                    fromEl.value = (toEl as HTMLTextAreaElement).value;
                }

                syncBooleanAttrProp(fromEl, toEl, 'disabled');

                return false;
            }

            if (fromEl.nodeName === "SCRIPT" && toEl.nodeName === "SCRIPT") {
                if (fromEl.src === toEl.src && fromEl.innerHTML === toEl.innerHTML) {
                    // Skip updating the script if the src and innerHTML are the same
                    // Firefox will re-execute the script if we replace it
                    return false;
                }

                const script = document.createElement('script');

                for (let i = 0; i < toEl.attributes.length; i++) {
                    const attr = toEl.attributes[i];
                    script.setAttribute(attr.name, attr.value);
                }

                script.innerHTML = toEl.innerHTML;
                fromEl.replaceWith(script)
                return false;
            }
        },
        onElUpdated(el) {
            if (el.nodeType === Node.ELEMENT_NODE) {
                el.dispatchEvent(new CustomEvent("wfc:updateElement", { bubbles: true, detail: {element: el, form} }));
            }
        },
        onBeforeNodeDiscarded(node) {
            if (node.tagName === "SCRIPT" && !options.updateScripts ||
                node.tagName === "STYLE" && !options.updateStyles ||
                node.tagName === "LINK" && node.hasAttribute('rel') && node.getAttribute('rel') === 'stylesheet' && !options.updateStyles) {
                return false;
            }

            if (node instanceof Element && node.hasAttribute('data-wfc-form')) {
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
        await submitForm(this, this);
    } else {
        originalSubmit.call(this);
    }
};

async function handleCallbacks(element) {
    const callbacks = JSON.parse(element.innerHTML);

    if (!callbacks || !Array.isArray(callbacks)) {
        return;
    }

    for (const callback of callbacks) {
        const {k: key, a: argument} = callback;

        if (key in callbackDefinitions) {
            const cb = callbackDefinitions[key];

            if (typeof cb === 'function') {
                try {
                    cb(argument);
                } catch (e) {
                    console.error(`Error executing callback ${key}`, e);
                }
            } else {
                console.warn(`Callback ${key} is not a function`, cb);
            }
        } else if (key in pendingCallbacks) {
            const arr = pendingCallbacks[key];

            if (arr.length < 100) {
                arr.push(argument);
            } else {
                console.warn(`Too many pending callbacks for ${key}, discarding callback`, callback);
            }
        } else {
            pendingCallbacks[key] = [argument];
        }
    }
}

document.addEventListener('DOMContentLoaded', async function() {
    const release = await postbackMutex.acquire();

    try {
        const callbacks = document.querySelectorAll('template[data-wfc-callbacks]');

        for (const callback of callbacks) {
            handleCallbacks(callback);
            callback.remove();
        }
    } finally {
        release();
    }
});

document.addEventListener('submit', async function(e){
    if (e.target instanceof Element && e.target.hasAttribute('data-wfc-form')) {
        e.preventDefault();
        await submitForm(e.target, e.target as HTMLFormElement);
    }
});

document.addEventListener('click', async function(e){
    if (!(e.target instanceof Element)) {
        return;
    }

    const postbackControl = e.target?.closest("[data-wfc-postback]");

    if (!postbackControl) {
        return;
    }

    e.preventDefault();

    const wfcDisabled = postbackControl.getAttribute('data-wfc-disabled');

    if (wfcDisabled === "true") {
        return;
    }

    await postBackElement(e.target);
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

function postBackChange(target: Element, timeOut = 1000, eventArgument: string = 'CHANGE', options?: PostBackOptions) {
    const container = getStreamPanel(target) ?? getForm(target);
    const eventTarget = target.getAttribute('name');
    const key = (container?.id ?? '') + eventTarget + eventArgument;

    if (timeouts[key]) {
        pendingPostbacks--;
        clearTimeout(timeouts[key]);
    }

    pendingPostbacks++;

    timeouts[key] = setTimeout(async () => {
        pendingPostbacks--;
        delete timeouts[key];
        await postBackElement(target, eventTarget, eventArgument, options);
    }, timeOut);
}

function postBack(target: Element, eventArgument?: string, options?: PostBackOptions) {
    const eventTarget = target.getAttribute('name');

    return postBackElement(target, eventTarget, eventArgument, options);
}

document.addEventListener('change', async function(e){
    if (e.target instanceof Element && e.target.hasAttribute('data-wfc-autopostback')) {
        postBackChange(e.target, 10);
    }
});

const wfc: WebFormsCore = {
    _callbacks: callbackDefinitions,
    _pendingCallbacks: pendingCallbacks,

    hiddenClass: '',
    postBackChange,
    postBack,

    retriggerLazy: async function(elementOrId: HTMLElement | string) {
        const element = typeof elementOrId === 'string'
            ? document.getElementById(elementOrId)
            : elementOrId;

        if (!element) {
            throw new Error(`Lazy loader element not found: ${elementOrId}`);
        }

        const uniqueId = element.getAttribute('data-wfc-lazy');

        if (uniqueId === null) {
            throw new Error('Element is not a lazy loader (missing data-wfc-lazy attribute)');
        }

        // If already loaded (empty value), read the UniqueID from the wfcForm hidden input
        let targetId = uniqueId;

        if (!targetId) {
            const formInput = element.querySelector<HTMLInputElement>('input[name="wfcForm"]');
            targetId = formInput?.value ?? '';
        }

        if (!targetId) {
            throw new Error('Cannot determine lazy loader UniqueID');
        }

        // Reset the attribute to signal "not loaded" so morphdom treats the response correctly
        element.setAttribute('data-wfc-lazy', targetId);
        element.setAttribute('aria-busy', 'true');

        await postBackElement(element, targetId, 'LAZY_LOAD', { validate: false });
    },

    get hasPendingPostbacks() {
        return pendingPostbacks > 0;
    },

    init: function (arg) {
        arg();
    },

    show: function(element: HTMLElement) {
        if (wfc.hiddenClass) {
            element.classList.remove(wfc.hiddenClass);
        } else {
            element.style.display = '';
        }
    },

    hide: function(element: HTMLElement) {
        if (wfc.hiddenClass) {
            element.classList.add(wfc.hiddenClass);
        } else {
            element.style.display = 'none';
        }
    },

    toggle: function(element: HTMLElement, value: boolean) {
        if (value) {
            wfc.show(element);
        } else {
            wfc.hide(element);
        }
    },

    validate: async function (validationGroup = "", serverOnly: boolean = false) {
        if (typeof validationGroup === "object" && validationGroup instanceof Element) {
            if (!validationGroup.hasAttribute('data-wfc-validate')) {
                return true;
            }

            validationGroup = validationGroup.getAttribute('data-wfc-validate') ?? "";
        }

        const validators: Array<(serverOnly: boolean) => (boolean | Promise<boolean>)> = [];
        const validatorsByElement = new Map<HTMLElement, Array<(serverOnly: boolean) => (boolean | Promise<boolean>)>>();
        const detail: WfcValidateEvent = {
            addValidator(validator, element) {
                if (element) {
                    if (!validatorsByElement.has(element)) {
                        validatorsByElement.set(element, []);
                    }

                    validatorsByElement.get(element).push(validator);
                } else {
                    validators.push(validator);
                }
            }
        }

        for (const element of document.querySelectorAll('[data-wfc-validate]')) {
            const elementValidationGroup = element.getAttribute('data-wfc-validate') ?? "";

            if (elementValidationGroup !== validationGroup) {
                continue;
            }

            element.dispatchEvent(new CustomEvent('wfc:validate', {
                bubbles: true,
                detail
            }));
        }

        let isValid = true;

        for (const validator of validators) {
            try {
                if (!await validator(serverOnly)) {
                    isValid = false;
                }
            } catch (e) {
                console.error('Validation error:', e);
                isValid = false;
            }
        }

        for (const [element, elementValidators] of validatorsByElement.entries()) {
            let isElementValid = true;

            for (const validator of elementValidators) {
                try {
                    if (!await validator(serverOnly)) {
                        isElementValid = false;
                    }
                } catch (e) {
                    console.error('Validation error:', e);
                    isElementValid = false;
                }
            }

            if (!isElementValid) {
                isValid = false;
            }

            element.dispatchEvent(new CustomEvent('wfc:elementValidated', {
                bubbles: true,
                detail: {
                    isValid: isElementValid,
                    element
                }
            }));
        }

        document.dispatchEvent(new CustomEvent('wfc:validated', {
            bubbles: true,
            detail: {
                isValid
            }
        }));

        return isValid;
    },

    bind: async function(selectors, options) {
        const init = (options.init ?? function() {}).bind(options);
        const update = (options.update ?? function() {}).bind(options)
        const afterUpdate = (options.afterUpdate ?? function() {}).bind(options);
        const submit = options.submit?.bind(options);
        const destroy = options.destroy?.bind(options);

        for (const element of document.querySelectorAll(selectors)) {
            await init(element);
            update(element, element);
            afterUpdate(element);
        }

        document.addEventListener('wfc:addElement', async function (e: CustomEvent) {
            const { element } = e.detail;

            if (element.matches(selectors)) {
                await init(element);
                update(element, element);
                afterUpdate(element);
            }
        });

        document.addEventListener('wfc:beforeUpdateElement', function (e: CustomEvent) {
            const { element, source } = e.detail;

            if (element.matches(selectors) && update(element, source)) {
                e.preventDefault();
            }
        });

        if (afterUpdate) {
            document.addEventListener('wfc:updateElement', function (e: CustomEvent) {
                const { element } = e.detail;

                if (element.matches(selectors)) {
                    afterUpdate(element);
                }
            });
        }

        if (submit) {
            document.addEventListener('wfc:beforeSubmit', function (e: CustomEvent<WfcBeforeSubmitEvent>) {
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
                }
            });
        }
    },

    registerCallback: async function(name, callback) {
        callbackDefinitions[name] = callback;

        if (!(name in pendingCallbacks)) {
            return;
        }

        const pending = pendingCallbacks[name];
        delete pendingCallbacks[name];

        for (const arg of pending) {
            try {
                callback(arg);
            } catch (e) {
                console.error(`Error executing callback ${name}`, e);
            }
        }
    },

    bindValidator: function(selectors, options) {
        type Props = {
            _isValid: boolean;
            _elementToValidate: HTMLElement | undefined;
            _callback: (e: CustomEvent) => void;
        }

        wfc.bind<Props>(selectors, {
            init: function(element) {
                element._isValid = true;

                if ('init' in options) {
                    options.init(element);
                }
            },
            afterUpdate: function(element) {
                // Restore old state
                const isValidStr = element.getAttribute('data-wfc-validated');

                if (isValidStr) {
                    element._isValid = isValidStr === 'true';
                } else {
                    wfc.toggle(element, !element._isValid);
                }

                // Bind to element
                const idToValidate = element.getAttribute('data-wfc-validator');

                if (!idToValidate) {
                    console.warn('No data-wfc-validator attribute found', element);
                    return;
                }

                const elementToValidate = document.getElementById(idToValidate);

                if (element._elementToValidate === elementToValidate) {
                    return;
                }

                this.destroy(element);
                element._elementToValidate = elementToValidate;

                if (!elementToValidate) {
                    console.warn(`Element with id ${idToValidate} not found`);
                    return;
                }

                element._callback = async function (e: CustomEvent<WfcValidateEvent>) {
                    e.detail.addValidator(async function(serverOnly) {
                        if (serverOnly) {
                            return element._isValid;
                        }

                        const disabled = element.hasAttribute('data-wfc-disabled');
                        const isValid = disabled || (options.validate ? await options.validate(elementToValidate, element) : this._isValid);
                        element._isValid = isValid;
                        wfc.toggle(element, !isValid);
                        return isValid;
                    }, elementToValidate);
                };

                elementToValidate.addEventListener('wfc:validate', element._callback);
            },
            destroy: function(element) {
                if (element._callback && element._elementToValidate) {
                    element._elementToValidate.removeEventListener('wfc:validate', element._callback);
                    element._callback = undefined;
                    element._elementToValidate = undefined;
                }
            }
        });
    },

    getStringValue: async (element: Element)=> {
        if ('getStringValue' in element) {
            return (await Promise.resolve(element.getStringValue()))?.toString() ?? "";
        } else if ('value' in element) {
            return element.value?.toString() ?? "";
        } else if ('textContent' in element) {
            return element.textContent ?? "";
        } else {
            return "";
        }
    },

    isEmpty: async (element: Element, initialValue: string = "") => {
        if ('isEmpty' in element) {
            const isEmpty = element.isEmpty;

            if (typeof isEmpty === "function") {
                const result = await Promise.resolve(isEmpty(initialValue));

                if (typeof(result) === "boolean") {
                    return result;
                }

                console.warn('isEmpty did not return a boolean', element);
            } else if (typeof isEmpty === "boolean") {
                return isEmpty;
            } else {
                console.warn('isEmpty is not a function', element);
            }
        }

        const value = await wfc.getStringValue(element);

        return initialValue === value;
    }
};

// Stream
wfc.bind('[data-wfc-stream]', {
    init: function(element) {
        const id = element.id;
        const baseElement = element.closest('[data-wfc-base]') as HTMLElement;

        const url = baseElement ? new URL(baseElement.getAttribute('data-wfc-base')) : location;
        let search = url.search;

        if (!search) {
            search = "?";
        } else {
            search += "&";
        }

        search += "__panel=" + id;

        const webSocket = new WebSocket(
            (url.protocol === "https:" ? "wss://" : "ws://") + url.host + url.pathname + search
        );

        element.webSocket = webSocket;
        element.isUpdating = false;

        webSocket.addEventListener('message', function(e) {
            const parser = new DOMParser();
            const index = e.data.indexOf('|');
            const options = getOptions(e.data.substring(0, index));
            const data = e.data.substring(index + 1);
            const htmlDoc = parser.parseFromString(sanitise(`<!DOCTYPE html><html><body>${data}</body></html>`, options), 'text/html');

            element.isUpdating = true;
            morphdom(element, htmlDoc.getElementById(id), getMorpdomSettings(options));
            element.isUpdating = false;
        });

        webSocket.addEventListener('open', function() {
            const formData = getFormData(element);
            webSocket.send(new URLSearchParams(formData as any).toString());
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

// Lazy loader: triggers postback after page load to replace skeletons with real content
// Use a WeakMap to signal between update and afterUpdate, because morphAttrs runs
// between them and removes any marker attributes we set on the DOM element.
const lazyRetriggerMap = new WeakMap<HTMLElement, string>();

wfc.bind('[data-wfc-lazy]', {
    init: async function(element: HTMLElement) {
        const uniqueId = element.getAttribute('data-wfc-lazy');

        // Only trigger postback if not yet loaded (non-empty value)
        if (!uniqueId) {
            return;
        }

        // Defer the postback to allow the page to finish loading
        setTimeout(async () => {
            await postBackElement(element, uniqueId, 'LAZY_LOAD', { validate: false });
        }, 0);
    },
    update: function(element: HTMLElement, source: HTMLElement) {
        const elementLazy = element.getAttribute('data-wfc-lazy');
        const sourceLazy = source.getAttribute('data-wfc-lazy');

        // When the server sends back an unloaded lazy loader (Retrigger),
        // allow morphdom to update it and mark it for a new lazy-load postback.
        if (elementLazy === '' && sourceLazy) {
            lazyRetriggerMap.set(element, sourceLazy);
        }
    },
    afterUpdate: function(element: HTMLElement) {
        const pendingId = lazyRetriggerMap.get(element);

        if (pendingId) {
            lazyRetriggerMap.delete(element);

            // Trigger a new lazy-load postback since the server retriggered this loader
            setTimeout(async () => {
                await postBackElement(element, pendingId, 'LAZY_LOAD', { validate: false });
            }, 0);
        }
    }
});

if ('wfc' in window) {
    const current = window.wfc;

    if ('hiddenClass' in current) {
        wfc.hiddenClass = current.hiddenClass;
    }

    window.wfc = wfc;

    if ('_' in current) {
        for (const bind of current._) {
            const [type, p1, p2] = bind;

            if (type === 0) {
                wfc.bind(p1, p2);
            } else if (type === 1) {
                wfc.bindValidator(p1, p2);
            } else if (type === 2) {
                wfc.init(p2);
            } else if (type === 3) {
                wfc.registerCallback(p1, p2);
            } else {
                console.warn('Unknown bind type', type);
            }
        }
    }
}

wfc.bindValidator('[data-wfc-requiredvalidator]', {
    validate: async function(elementToValidate: HTMLInputElement, validator) {
        return !(await wfc.isEmpty(elementToValidate, validator.getAttribute('data-wfc-requiredvalidator')));
    }
});

wfc.bindValidator('[data-wfc-customvalidator]', {
    validate: function() {
        return true;
    }
});

window.wfc = wfc;

