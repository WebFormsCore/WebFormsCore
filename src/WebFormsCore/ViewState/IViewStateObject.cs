namespace WebFormsCore
{
    public interface IViewStateObject
    {
        bool WriteToViewState { get; }

        void TrackViewState(ViewStateProvider provider);

        void WriteViewState(ref ViewStateWriter writer);

        void ReadViewState(ref ViewStateReader reader);
    }
}
