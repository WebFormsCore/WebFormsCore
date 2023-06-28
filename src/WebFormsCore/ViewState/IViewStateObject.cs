namespace WebFormsCore
{
    public interface IViewStateObject
    {
        bool WriteToViewState { get; }

        void TrackViewState();

        void WriteViewState(ref ViewStateWriter writer);

        void ReadViewState(ref ViewStateReader reader);
    }
}
