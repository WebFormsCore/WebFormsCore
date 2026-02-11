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

            // Prevent default behavior to handle switching manually.
            e.preventDefault();
            e.stopPropagation();

            const lazyLoader = panel.querySelector<HTMLElement>('[data-wfc-lazy]');

            // Always switch visually first
            switchTab(element, button, panel);

            if (button.hasAttribute('data-wfc-tab-autopostback')) {
                wfc.postBack(element);
            } else if (lazyLoader && lazyLoader.getAttribute('aria-busy') === 'true') {
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

                const tabs = Array.from(element.querySelectorAll<HTMLElement>('[role="tab"]:not([disabled])'));
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
