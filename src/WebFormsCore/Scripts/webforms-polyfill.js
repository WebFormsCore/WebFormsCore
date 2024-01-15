(function () {
    'use strict';

    if (window.Sys === undefined) {
        const pageRequestManager = {
            add_beginRequest: function (func) {
                document.addEventListener("wfc:beforeSubmit", func, false);
            },
            remove_beginRequest: function (func) {
                document.removeEventListener("wfc:beforeSubmit", func, false);
            },
            add_endRequest: function (func) {
                document.addEventListener("wfc:afterSubmit", func, false);
            },
            remove_endRequest: function (func) {
                document.removeEventListener("wfc:afterSubmit", func, false);
            }
        };
        window.Sys = {
            WebForms: {
                PageRequestManager: {
                    getInstance: () => pageRequestManager
                }
            }
        };
    }

})();
