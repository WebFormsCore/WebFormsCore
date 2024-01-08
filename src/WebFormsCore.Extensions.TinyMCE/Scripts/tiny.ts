import tinymce, { Editor } from 'tinymce';

import 'tinymce/icons/default';

/* Import plugins */
import 'tinymce/models/dom';

import 'tinymce/themes/silver';

import 'tinymce/plugins/advlist';
import 'tinymce/plugins/code';
import 'tinymce/plugins/link';
import 'tinymce/plugins/lists';
import 'tinymce/plugins/table';

import contentUiCss from 'tinymce/skins/ui/oxide/content.css';
import contentCss from 'tinymce/skins/content/default/content.css';

document.addEventListener('wfc:discardElement', function(e) {
    const element = e.target as HTMLElement;

    // Prevent deleting the div in the body that contains the editor modals and popups
    if (element.classList.contains('tox')) {
        e.preventDefault();
    }
});

type Props = {
    _editor: Editor;
};

wfc.bind<Props>(".js-tinymce", {
    init: async function(element) {
        const textArea = element.querySelector('textarea');
        const options = JSON.parse(element.getAttribute('data-options') || '{}');

        const editor = await tinymce.init({
            target: textArea,
            plugins: 'advlist code link lists table',
            skin: false,
            content_css: false,
            content_style: contentUiCss.toString() + '\n' + contentCss.toString(),
            branding: true,
            promotion: false,
            ...options
        });

        element._editor = editor[0];
        element.removeAttribute('style');
    },
    update: function(element, newElement) {
        const newTextArea= newElement.querySelector('textarea');
        const editor = element._editor;

        if (newTextArea.innerText) {
            editor.setContent(newTextArea.innerText);
        }

        editor.readonly = newTextArea.hasAttribute('disabled');

        return true;
    },
    submit: function(element, data) {
        const textArea = element.querySelector('textarea');
        data.set(textArea.name, element._editor.getContent());
    },
    destroy: function(element) {
        element._editor.destroy();
    }
});
