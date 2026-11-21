namespace Mappy.TerrainBrush
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// One set of river or ridge pieces in a world, such as River, RiverB or Ridge,
    /// keyed by the cell's corner layout and the sides the path runs through.
    /// A style missing a piece borrows it from its fallback, the largest style
    /// of the same kind.
    /// </summary>
    public sealed class TerrainPathStyle
    {
        private readonly Dictionary<PieceKey, List<TerrainSectionRef>> pieces =
            new Dictionary<PieceKey, List<TerrainSectionRef>>();

        private readonly HashSet<TerrainWorld.LayoutKey> layouts = new HashSet<TerrainWorld.LayoutKey>();

        private bool thumbnailIsStraight;

        public TerrainPathStyle(string name, TerrainPathKind kind)
        {
            this.Name = name;
            this.Kind = kind;
        }

        public string Name { get; }

        public TerrainPathKind Kind { get; }

        public TerrainPathStyle Fallback { get; internal set; }

        public int PieceCount { get; private set; }

        /// <summary>
        /// A piece that shows what the style looks like: a straight run if there is one.
        /// </summary>
        public TerrainSectionRef Thumbnail { get; private set; }

        /// <summary>
        /// True when the style or its fallback has any piece for this corner layout.
        /// </summary>
        public bool Accepts(int[] corners)
        {
            var key = Layout(corners);
            return this.layouts.Contains(key) || (this.Fallback != null && this.Fallback.layouts.Contains(key));
        }

        /// <summary>
        /// The pieces for a cell, or null. A river has no end pieces, so a
        /// dead end uses the straight piece that runs on through it.
        /// </summary>
        public IReadOnlyList<TerrainSectionRef> Sections(int[] corners, int mask)
        {
            var found = this.Exact(corners, mask);
            if (found != null)
            {
                return found;
            }

            if (TerrainSides.Count(mask) == 1 && IsFlat(corners))
            {
                return this.Exact(corners, mask | TerrainSides.Opposite(mask));
            }

            return null;
        }

        public void Add(int[] corners, int mask, TerrainSectionRef section)
        {
            var key = new PieceKey(Layout(corners), mask);
            List<TerrainSectionRef> list;
            if (!this.pieces.TryGetValue(key, out list))
            {
                list = new List<TerrainSectionRef>();
                this.pieces.Add(key, list);
            }

            list.Add(section);
            this.layouts.Add(key.Layout);
            this.PieceCount++;
            var straight = IsFlat(corners) && mask == (TerrainSides.Left | TerrainSides.Right);
            if (this.Thumbnail == null || (straight && !this.thumbnailIsStraight))
            {
                this.Thumbnail = section;
                this.thumbnailIsStraight = straight;
            }
        }

        private static bool IsFlat(int[] corners)
        {
            return corners[0] == corners[1] && corners[0] == corners[2] && corners[0] == corners[3];
        }

        private IReadOnlyList<TerrainSectionRef> Exact(int[] corners, int mask)
        {
            var key = new PieceKey(Layout(corners), mask);
            List<TerrainSectionRef> list;
            if (this.pieces.TryGetValue(key, out list))
            {
                return list;
            }

            if (this.Fallback != null && this.Fallback.pieces.TryGetValue(key, out list))
            {
                return list;
            }

            return null;
        }

        private static TerrainWorld.LayoutKey Layout(int[] corners)
        {
            if (corners == null || corners.Length != TerrainCorners.Count)
            {
                throw new ArgumentException("A layout has four corners.", nameof(corners));
            }

            return new TerrainWorld.LayoutKey(corners[0], corners[1], corners[2], corners[3]);
        }

        private struct PieceKey : IEquatable<PieceKey>
        {
            public PieceKey(TerrainWorld.LayoutKey layout, int mask)
            {
                this.Layout = layout;
                this.Mask = mask;
            }

            public TerrainWorld.LayoutKey Layout { get; }

            public int Mask { get; }

            public bool Equals(PieceKey other)
            {
                return this.Layout.Equals(other.Layout) && this.Mask == other.Mask;
            }

            public override bool Equals(object obj)
            {
                return obj is PieceKey other && this.Equals(other);
            }

            public override int GetHashCode()
            {
                return (this.Layout.GetHashCode() * 397) ^ this.Mask;
            }
        }
    }
}
