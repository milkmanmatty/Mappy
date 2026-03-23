namespace Mappy.Operations
{
    using System;
    using System.Drawing;

    using Mappy.Models;

    public class ChangeStartPositionOperation : IReplayableOperation
    {
        private Point? oldPosition;

        public ChangeStartPositionOperation(IMapModel map, int schemaIndex, int startSlotIndex, Point newPosition)
        {
            this.Map = map;
            this.SchemaIndex = schemaIndex;
            this.StartSlotIndex = startSlotIndex;
            this.NewPosition = newPosition;
        }

        public IMapModel Map { get; }

        public int SchemaIndex { get; }

        public int StartSlotIndex { get; }

        public Point NewPosition { get; }

        public void Execute()
        {
            this.oldPosition = this.Map.Attributes.GetStartPosition(this.SchemaIndex, this.StartSlotIndex);
            this.Map.Attributes.SetStartPosition(this.SchemaIndex, this.StartSlotIndex, this.NewPosition);
        }

        public void Undo()
        {
            this.Map.Attributes.SetStartPosition(this.SchemaIndex, this.StartSlotIndex, this.oldPosition);
        }

        public ChangeStartPositionOperation Combine(ChangeStartPositionOperation newOp)
        {
            if (newOp.SchemaIndex != this.SchemaIndex || newOp.StartSlotIndex != this.StartSlotIndex)
            {
                throw new ArgumentException("other op must be for same start position");
            }

            var n = new ChangeStartPositionOperation(this.Map, this.SchemaIndex, this.StartSlotIndex, newOp.NewPosition);
            n.oldPosition = this.oldPosition;
            return n;
        }
    }
}
