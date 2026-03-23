namespace Mappy.UI.Tags
{
    using Mappy.Services;

    public class StartPositionTag : IMapItemTag
    {
        public StartPositionTag(int schemaIndex, int startSlotIndex)
        {
            this.SchemaIndex = schemaIndex;
            this.StartSlotIndex = startSlotIndex;
        }

        public int SchemaIndex { get; }

        public int StartSlotIndex { get; }

        public void SelectItem(Dispatcher dispatcher)
        {
            dispatcher.SelectStartPosition(this.SchemaIndex, this.StartSlotIndex);
        }
    }
}
