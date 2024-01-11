using WebFormsCore.UI;

namespace Application;

public partial class Test : Control
{
    public string? Message
    {
        get => LibraryControl.Message;
        set => LibraryControl.Message = value;
    }
}
