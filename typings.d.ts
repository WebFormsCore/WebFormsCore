export interface WebFormsCore {
    _callbacks: { [name: string]: (arg: any) => void | Promise<void> };
    _pendingCallbacks: { [name: string]: any[] };

    _?: [number, string, any][];
    hiddenClass: string;

    readonly hasPendingPostbacks: boolean;
    postBackChange: (target: Element, timeOut?: number, eventArgument?: string, options?: PostBackOptions) => void;
    postBack: (target: Element, eventArgument?: string, options?: PostBackOptions) => Promise<void>;
    retriggerLazy: (elementOrId: HTMLElement | string) => Promise<void>;

    show: (element: HTMLElement) => void;
    hide: (element: HTMLElement) => void;
    toggle: (element: HTMLElement, value: boolean) => void;

    init: (arg: Function) => void;

    bind: <T = {}>(selectors: string, options: {
        init?: (element: HTMLElement & T) => void | Promise<void>;
        update?: (element: HTMLElement & T, source: HTMLElement) => boolean | void;
        afterUpdate?: (element: HTMLElement & T) => void;
        submit?: (element: HTMLElement & T, formData: FormData) => void;
        destroy?: (element: HTMLElement & T) => void;
    }) => void;

    bindValidator: (selectors: string, options: {
        init?: (element: HTMLElement) => void;
        validate?: (elementToValidate: HTMLElement, validator: HTMLElement) => boolean | Promise<boolean>
    }) => void;

    registerCallback: (name: string, callback: (arg: any) => void | Promise<void>) => void;
    validate: (validationGroup?: string | Element, serverOnly?: boolean) => Promise<boolean>;
    getStringValue: (element: Element) => Promise<string>;
    isEmpty: (element: Element, initialValue?: string) => Promise<boolean | null>;
}

export interface PostBackOptions {
    validate?: boolean;
    signal?: AbortSignal;
}

export interface PageRequestManager {
    add_beginRequest(handler: () => void): void;
    remove_beginRequest(handler: () => void): void;
    add_endRequest(handler: () => void): void;
    remove_endRequest(handler: () => void): void;
}

export interface Sys {
    WebForms: {
        PageRequestManager: {
            getInstance: () => PageRequestManager;
        }
    }
}

export interface ViewStateContainer {
    formData: FormData;
    element: Element;

    querySelectorAll(selectors: string): Array<Element>;
    querySelector(selector: string): Element | null;
    addInputs(selector: string): void;
}

export interface WfcBeforeSubmitEvent {
    target: HTMLElement,
    container: ViewStateContainer,
    eventTarget: string,
    element: Element

    addRequestInterceptor(interceptor: ((request: RequestInit) => void | Promise<void>)): void;
}

export interface WfcValidateEvent {
    addValidator(validator: (serverOnly: boolean) => (boolean | Promise<boolean>), element?: HTMLElement): void;
}

declare global {
    var wfc: WebFormsCore
    var Sys: Sys

    interface Window {
        wfc: WebFormsCore;
        Sys: Sys;
    }

    interface WebFormsCoreEvent {
        addEventListener(event: "wfc:beforeSubmit", listener: (this: this, ev: CustomEvent<WfcBeforeSubmitEvent>) => any, options?: boolean | AddEventListenerOptions): void;
        addEventListener(event: "wfc:validate", listener: (this: this, ev: CustomEvent<WfcValidateEvent>) => any, options?: boolean | AddEventListenerOptions): void;
        addEventListener(event: "wfc:elementValidated", listener: (this: this, ev: CustomEvent<{ element: Element, valid: boolean }>) => any, options?: boolean | AddEventListenerOptions): void;
        addEventListener(event: "wfc:validated", listener: (this: this, ev: CustomEvent<{ valid: boolean }>) => any, options?: boolean | AddEventListenerOptions): void;
    }
    
    interface Document extends WebFormsCoreEvent {
    }

    interface Element extends WebFormsCoreEvent {
        webSocket: WebSocket | undefined;
        isUpdating: boolean;
        dispatchEvent(event: Event): boolean;
        isEmpty?: boolean | ((initialValue: string) => boolean) | ((initialValue: string) => Promise<boolean>);
        getStringValue?: (() => string) | (() => Promise<string>);
    }

    var trustedTypes: {
        createPolicy(name: string, policy: {
            createHTML?: (input: string) => string;
            createScript?: (input: string) => string;
            createScriptURL?: (input: string) => string;
        }): void;
    }
}