namespace Mappy.Models
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

    public sealed class MarkerLine
    {
        private readonly Point[] points;

        public MarkerLine(Guid id, Point start, Point end)
            : this(id, new[] { start, end })
        {
        }

        public MarkerLine(Guid id, IReadOnlyList<Point> points)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            if (points.Count < 2)
            {
                throw new ArgumentException("A marker line needs at least two points.", nameof(points));
            }

            this.Id = id;
            this.points = points.ToArray();
        }

        public Guid Id { get; }

        public IReadOnlyList<Point> Points => this.points;

        public Point Start => this.points[0];

        public Point End => this.points[this.points.Length - 1];
    }
}
