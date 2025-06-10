using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Controls.CustomValidators.Pages;

public partial class TextBoxPage : Page
{
    protected override ValueTask OnInitAsync(CancellationToken token)
    {
        labelPostback.Text = IsPostBack ? "True" : "False";

        return base.OnInitAsync(token);
    }

    protected Task button_OnClick(Button sender, EventArgs e)
    {
        labelValue.Text = string.IsNullOrWhiteSpace(textBox.Text) ? "Empty" : textBox.Text;
        return Task.CompletedTask;
    }

    protected Task validator_OnServerValidate(CustomValidator sender, ServerValidateEventArgs e)
    {
        e.IsValid = !string.Equals(textBox.Text, "invalid", StringComparison.OrdinalIgnoreCase);
        return Task.CompletedTask;
    }
}
