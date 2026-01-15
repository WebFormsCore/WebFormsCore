namespace WebFormsCore.UI;

public class Ref<T>
{
    public T Value { get; set; } = default!;
}

public static class ControlEventExtensions
{
    extension<T>(T control) where T : Control
    {
        public Ref<T> Ref
        {
            set => value.Value = control;
        }
    }
}
