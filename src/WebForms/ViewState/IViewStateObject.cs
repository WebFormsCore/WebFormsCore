namespace System.Web
{
    public interface IViewStateObject
    {
        void WriteViewState(ref ViewStateWriter writer);

        void ReadViewState(ref ViewStateReader reader);
    }
}
