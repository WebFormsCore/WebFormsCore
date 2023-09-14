using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WebFormsCore.UI.Attributes;

namespace WebFormsCore.UI.WebControls.Internal;

[ParseChildren(true)]
public abstract class ChoicesBase : Control
{
    protected override void OnInit(EventArgs args)
    {
        base.OnInit(args);

        Page.ClientScript.RegisterStartupStyleLink(typeof(Choices), "Choices", "https://cdn.jsdelivr.net/npm/choices.js/public/assets/styles/choices.min.css");
        Page.ClientScript.RegisterStartupScriptLink(typeof(Choices), "Choices", "https://cdn.jsdelivr.net/npm/choices.js/public/assets/scripts/choices.min.js");
        Page.ClientScript.RegisterStartupScript(typeof(Choices), "ChoicesStartup", """
            WebFormsCore.bind(".js-choice", {
                init: function(element) {
                    const input = element.querySelector('input,select');
                    const choice = new Choices(input, {
                        allowHTML: true,
                        removeItemButton: true
                    });

                    element.classList.remove('choices__inner');
                    element.input = input;
                    element.choice = choice;
                    element.autoPostBack = false;

                    input.addEventListener('change', function () {
                        if (element.autoPostBack) {
                            WebFormsCore.postBackChange(input, 50);
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
                        // TODO: Update select options
                        
                        const newValues = Array.from(newInput.options).filter(x => x.selected).map(x => x.value);
                        const currentValues = choice.getValue(true);
                        const currentValuesArray = currentValues ? Array.isArray(currentValues) ? currentValues : [currentValues] : [];
                        
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
            """);
    }

    protected override void OnPreRender(EventArgs args)
    {
        if (Page.Csp.Enabled)
        {
            var options = Context.RequestServices.GetService<IOptions<WebFormsCoreOptions>>();

            if (options?.Value.HiddenClass is null) Page.Csp.StyleSrc.AddUnsafeInlineHash("display:none;");
            Page.Csp.ImgSrc.Add("data:");
        }

        base.OnPreRender(args);
    }
}
