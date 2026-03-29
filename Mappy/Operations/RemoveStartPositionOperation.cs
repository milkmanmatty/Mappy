namespace Mappy.Operations
{
    using System.Drawing;

    using Mappy.Models;

    public class RemoveStartPositionOperation : IReplayableOperation
    {
        private Point? oldPosition;

        public RemoveStartPositionOperation(IMapModel map, int schemaIndex, int startSlotIndex)
        {
            this.Map = map;
            this.SchemaIndex = schemaIndex;
            this.StartSlotIndex = startSlotIndex;
        }

        public IMapModel Map { get; }

        public int SchemaIndex { get; }

        public int StartSlotIndex { get; }

        public void Execute()
        {
            this.oldPosition = this.Map.Attributes.GetStartPosition(this.SchemaIndex, this.StartSlotIndex);
            this.Map.Attributes.SetStartPosition(this.SchemaIndex, this.StartSlotIndex, null);
        }

        public void Undo()
        {
            this.Map.Attributes.SetStartPosition(this.SchemaIndex, this.StartSlotIndex, this.oldPosition);
        }
    }
}
