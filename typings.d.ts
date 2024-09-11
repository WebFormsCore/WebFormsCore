export interface WebFormsCore {
    _?: [number, string, any][];
    hiddenClass: string;

    readonly hasPendingPostbacks: boolean;
    postBackChange: (target: Element, timeOut?: number, eventArgument?: string) => void;
    postBack: (target: Element, eventArgument?: string) => Promise<void>;

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

    bindValidator: (selectors: string, validate: (element: HTMLElement, source: HTMLElement) => boolean) => void;

    validate: (validationGroup?: string | Element) => boolean;
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

declare global {
    var wfc: WebFormsCore
    var Sys: Sys

    interface Window {
        wfc: WebFormsCore;
        Sys: Sys;
    }

    interface Element {
        webSocket: WebSocket | undefined;
        isUpdating: boolean;
    }

    var trustedTypes: {
        createPolicy(name: string, policy: {
            createHTML?: (input: string) => string;
            createScript?: (input: string) => string;
            createScriptURL?: (input: string) => string;
        }): void;
    }
}