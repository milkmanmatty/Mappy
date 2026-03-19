using Mappy.Models.Enums;

namespace Mappy.Operations
{
    using Mappy.Collections;

    public class FlipAreaOperation<T> : IReplayableOperation
    {
        private readonly IGrid<T> source;
        private readonly IGrid<T> dest;

        private readonly int sourceX;
        private readonly int sourceY;

        private readonly int destX;
        private readonly int destY;

        private readonly int width;
        private readonly int height;

        private readonly FlipDirection direction;

        private IGrid<T> oldContents;

        public FlipAreaOperation(IGrid<T> source, IGrid<T> dest, int sourceX, int sourceY, int destX, int destY, int width, int height, FlipDirection direction)
        {
            this.source = source;
            this.dest = dest;
            this.sourceX = sourceX;
            this.sourceY = sourceY;
            this.destX = destX;
            this.destY = destY;
            this.width = width;
            this.height = height;
            this.direction = direction;

            this.oldContents = new Grid<T>(this.width, this.height);

            GridMethods.Copy(this.source, this.oldContents, sourceX, sourceY, 0, 0, width, height);
        }

        public void Execute()
        {
            GridMethods.FlipArea(this.source, this.dest, this.sourceX, this.sourceY, this.destX, this.destY, this.width, this.height, this.direction);
        }

        public void Undo()
        {
            GridMethods.Copy(this.oldContents, this.source, this.sourceX, this.sourceY);
        }
    }
}