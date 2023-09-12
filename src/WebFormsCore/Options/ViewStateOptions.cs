namespace WebFormsCore;

public class ViewStateOptions
{
    public bool Enabled { get; set; } = true;

    public string? EncryptionKey { get; set; }

    public int MaxBytes { get; set; } = 102400;

    public bool Debug { get; set; }
}
