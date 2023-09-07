namespace WebFormsCore;

public class ViewStateOptions
{
    public bool Enabled { get; set; } = true;

    public string EncryptionKey { get; set; } = string.Empty;

    public int MaxBytes { get; set; } = 102400;
}
