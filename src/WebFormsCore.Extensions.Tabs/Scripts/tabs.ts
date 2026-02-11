/// <reference path="../../../../typings.d.ts" />

function switchTab(container: HTMLElement, button: HTMLElement, panel: HTMLElement) {
    // Deselect all tab buttons and set tabindex="-1"
    const buttons = container.querySelectorAll<HTMLElement>('[role="tab"]');
    for (const btn of buttons) {
        btn.setAttribute('aria-selected', 'false');
        btn.setAttribute('tabindex', '-1');
    }

    // Hide all panels
    const panels = container.querySelectorAll<HTMLElement>('[role="tabpanel"]');
    for (const p of panels) {
        p.style.display = 'none';
        p.setAttribute('aria-hidden', 'true');
    }

    // Activate the selected tab
    button.setAttribute('aria-selected', 'true');
    button.setAttribute('tabindex', '0');
    panel.style.display = '';
    panel.removeAttribute('aria-hidden');
}

function syncVisibility(container: HTMLElement) {
    const buttons = container.querySelectorAll<HTMLElement>('[role="tab"]');

    for (const btn of buttons) {
        const panelId = btn.getAttribute('aria-controls');
        if (!panelId) continue;

        const panel = document.getElementById(panelId);
        if (!panel) continue;

        const isActive = btn.getAttribute('aria-selected') === 'true';

        if (isActive) {
            panel.style.display = '';
            panel.removeAttribute('aria-hidden');
            btn.setAttribute('tabindex', '0');
        } else {
            panel.style.display = 'none';
            panel.setAttribute('aria-hidden', 'true');
            btn.setAttribute('tabindex', '-1');
        }
    }
}

/**
 * Returns an ordered array of non-disabled tab buttons inside the tablist.
 */
function getEnabledTabs(container: HTMLElement): HTMLElement[] {
    const all = container.querySelectorAll<HTMLElement>('[role="tab"]:not([disabled])');
    return Array.from(all);
}

/**
 * Returns the LazyLoader element inside a tab panel, or null if the panel
 * is not lazy or is already loaded.
 */
function getUnloadedLazyLoader(panel: HTMLElement): HTMLElement | null {
    const loader = panel.querySelector<HTMLElement>('[data-wfc-lazy]');
    if (!loader) return null;
    // aria-busy="true" means the lazy loader has not loaded yet
    return loader.getAttribute('aria-busy') === 'true' ? loader : null;
}

wfc.bind('[data-wfc-tabs]', {
    init: function (element: HTMLElement) {
        // Click handler for tab activation
        element.addEventListener('click', function (e: MouseEvent) {
            if (!(e.target instanceof Element)) return;

            const button = e.target.closest<HTMLElement>('[role="tab"]');
            if (!button) return;

            // Disabled tab: do nothing
            if (button.hasAttribute('disabled')) {
                e.preventDefault();
                return;
            }

            const panelId = button.getAttribute('aria-controls');
            if (!panelId) return;

            const panel = document.getElementById(panelId);
            if (!panel) return;

            // Always prevent default and stop propagation — tab buttons no
            // longer use data-wfc-postback; all switching is handled here.
            e.preventDefault();
            e.stopPropagation();

            const lazyLoader = getUnloadedLazyLoader(panel);

            // Always switch visually first
            switchTab(element, button, panel);

            if (button.hasAttribute('data-wfc-tab-autopostback')) {
                // AutoPostBack: trigger a full page postback so the server
                // processes the tab change immediately.
                wfc.postBack(element);
            } else if (lazyLoader) {
                // Lazy tab not yet loaded: trigger a scoped postback via the
                // LazyLoader so only the tab content section is loaded.
                wfc.retriggerLazy(lazyLoader).then(function () {
                    syncVisibility(element);
                });
            }
        });

        // Keyboard handler for arrow key navigation (on the tablist)
        const tablist = element.querySelector<HTMLElement>('[role="tablist"]');
        if (tablist) {
            tablist.addEventListener('keydown', function (e: KeyboardEvent) {
                const currentTab = (e.target as HTMLElement).closest<HTMLElement>('[role="tab"]');
                if (!currentTab) return;

                const tabs = getEnabledTabs(element);
                const currentIndex = tabs.indexOf(currentTab);
                if (currentIndex === -1) return;

                let newIndex: number;

                switch (e.key) {
                    case 'ArrowRight':
                        newIndex = (currentIndex + 1) % tabs.length;
                        break;
                    case 'ArrowLeft':
                        newIndex = (currentIndex - 1 + tabs.length) % tabs.length;
                        break;
                    case 'Home':
                        newIndex = 0;
                        break;
                    case 'End':
                        newIndex = tabs.length - 1;
                        break;
                    default:
                        return;
                }

                e.preventDefault();
                e.stopPropagation();
                tabs[newIndex].focus();
            });
        }
    },

    submit: function (element: HTMLElement, data: FormData) {
        const name = element.getAttribute('data-wfc-tab-name');
        if (!name) return;

        const activeButton = element.querySelector<HTMLElement>('[role="tab"][aria-selected="true"]');
        if (!activeButton) return;

        const index = activeButton.getAttribute('data-wfc-tab-index');
        if (index !== null) {
            data.set(name, index);
        }
    },

    afterUpdate: function (element: HTMLElement) {
        syncVisibility(element);
    }
});
