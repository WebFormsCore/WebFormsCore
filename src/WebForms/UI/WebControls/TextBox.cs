namespace System.Web.UI.WebControls;

public class TextBox : Control
{
    public string? Text
    {
        get => ViewState[nameof(Text)] as string;
        set => ViewState[nameof(Text)] = value;
    }
}
