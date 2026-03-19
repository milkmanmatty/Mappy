namespace Mappy.Operations
{
    using System;
    using System.Drawing;
    using Mappy.Collections;
    using Mappy.Models.Enums;

    public class FlipAreaOperation<T> : IReplayableOperation
    {
        private readonly IGrid<T> source;
        private readonly IGrid<T> dest;

        private readonly int width;
        private readonly int height;

        private readonly FlipDirection direction;

        private readonly IGrid<T> oldContents;

        public FlipAreaOperation(IGrid<T> source, IGrid<T> dest, int width, int height, FlipDirection direction)
        {
            Type type = typeof(T);
            if (type != typeof(int) && type != typeof(Bitmap) && type != typeof(double))
            {
                throw new NotSupportedException($"The type {type.Name} is not supported. Only int, float, and double are allowed.");
            }

            this.source = source;
            this.dest = dest;
            this.width = width;
            this.height = height;
            this.direction = direction;

            this.oldContents = new Grid<T>(this.width, this.height);

            GridMethods.Copy(this.source, this.oldContents, 0, 0, 0, 0, width, height);
        }

        public void Execute()
        {
            Type type = typeof(T);

            // Dirty but it'll do for now
            if (type != typeof(int))
            {
                GridMethods.FlipArea((IGrid<int>)this.source, (IGrid<int>)this.dest, this.width, this.height, this.direction);
            }
            else if (type != typeof(Bitmap))
            {
                GridMethods.FlipArea((IGrid<Bitmap>)this.source, (IGrid<Bitmap>)this.dest, this.width, this.height, this.direction);
            }
        }

        public void Undo()
        {
            GridMethods.Copy(this.oldContents, this.dest, 0, 0);
        }
    }
}