using WebFormsCore.UI;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Controls.Forms.Pages;

public partial class DynamicForms : Page, IPostBackAsyncLoadHandler
{
    [ViewState] private bool _createForm;

    protected override void OnInit(EventArgs e)
    {
        base.OnInit(e);
    }

    private async Task InitFormAsync()
    {
        var form = new HtmlForm();
        await Controls.AddAsync(form);

        var button = WebActivator.CreateControl<LinkButton>();
        await form.Controls.AddAsync(button);

        button.Text = "Submit";
        button.ClientIDMode = ClientIDMode.Static;
        button.ClientID = "dynamicButton";
        button.Click += (sender, args) =>
        {
            var that = this;

            sender.Text = "Clicked";
            return Task.CompletedTask;
        };
    }

    protected async Task btnSubmit_Click(LinkButton sender, EventArgs e)
    {
        _createForm = true;
        await InitFormAsync();
    }

    public async Task AfterPostBackLoadAsync()
    {
        if (_createForm)
        {
            await InitFormAsync();
        }
    }
}
