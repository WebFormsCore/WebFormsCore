namespace WebFormsCore.Options;

public class ViewStateOptions
{
    public bool Enabled { get; set; } = true;

    public string EncryptionKey { get; set; } = string.Empty;
}
