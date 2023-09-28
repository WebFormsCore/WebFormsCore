export interface WebFormsCore {
    _?: [number, string, any][];
    hiddenClass: string;

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

    validate: (validationGroup?: string) => boolean;
}

declare global {
    var wfc: WebFormsCore

    interface Window {
        wfc: WebFormsCore;
    }

    interface Element {
        webSocket: WebSocket | undefined;
        isUpdating: boolean;
    }
}