/// <reference types="../../../typings.d.ts" />
import Choices from 'choices.js'

type Props = {
    input: HTMLInputElement | HTMLSelectElement;
    choice: Choices;
    autoPostBack: boolean;
};

wfc.bind<Props>(".js-choice", {
    init: function(element) {
        // Remove the temp element to prevent page shifting
        element.classList.remove('choices__inner');

        const tempInput = element.querySelector('.js-choice-temp');
        if (tempInput) {
            tempInput.remove();
        }

        // Initialize choices
        const input = element.querySelector('input,select');
        const choice = new Choices(input, {
            allowHTML: true,
            removeItemButton: true
        });

        element.input = input as HTMLInputElement | HTMLSelectElement;
        element.choice = choice;
        element.autoPostBack = false;

        input.addEventListener('change', function () {
            if (element.autoPostBack) {
                wfc.postBackChange(input, 50);
            }
        });
    },
    update: function(element, newElement) {
        const { choice, input } = element;
        const newInput = newElement.querySelector('input,select');

        // Auto post back
        element.autoPostBack = newElement.hasAttribute('data-wfc-autopostback');

        // Set disabled
        if (newElement.hasAttribute('data-wfc-disabled')) {
            choice.disable();
        } else {
            choice.enable();
        }

        // Update input value
        if (input.tagName === 'INPUT') {
            const json = newElement.getAttribute('data-value');

            if (json) {
                const values = JSON.parse(json);

                choice.clearStore();
                choice.setValue(values);
            }
        }

        // Update select options
        if (input.tagName === 'SELECT') {
            const newSelect = newInput as HTMLSelectElement;
            // TODO: Update select options

            const newValues = Array.from(newSelect.options).filter(x => x.selected).map(x => x.value);
            const currentValues = choice.getValue(true);
            const currentValuesArray = (currentValues ? Array.isArray(currentValues) ? currentValues : [currentValues] : []) as string[];

            for (const value of currentValuesArray) {
                if (!newValues.includes(value)) {
                    choice.removeActiveItemsByValue(value);
                }
            }

            for (const value of newValues) {
                if (!currentValuesArray.includes(value)) {
                    choice.setChoiceByValue(value);
                }
            }
        }

        return true;
    },
    submit: function(element, data) {
        const { choice, input } = element;

        data.set(input.name, JSON.stringify(choice.getValue(true)));
    },
    destroy: function(element) {
        const { choice } = element;

        choice.destroy();
    }
});