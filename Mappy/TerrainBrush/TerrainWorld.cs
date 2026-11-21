namespace Mappy.TerrainBrush
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// One brush world: its base terrains, the transition layouts that join them,
    /// and the distance between two bases along those joins.
    /// </summary>
    public sealed class TerrainWorld
    {
        public const int Far = 1000000;

        private readonly Dictionary<LayoutKey, TerrainSectionRef[]> layouts;

        private readonly int[,] distance;

        private readonly int[,] stepToward;

        private readonly Dictionary<LayoutKey, TerrainSectionRef[]> slopes;

        private readonly Dictionary<LayoutKey, TerrainSectionRef[]> plainSlopes;

        internal TerrainWorld(
            string name,
            IReadOnlyList<TerrainBase> bases,
            Dictionary<LayoutKey, TerrainSectionRef[]> layouts,
            int[,] distance,
            int[,] stepToward)
            : this(
                name,
                bases,
                layouts,
                distance,
                stepToward,
                new Dictionary<LayoutKey, TerrainSectionRef[]>(),
                new Dictionary<LayoutKey, TerrainSectionRef[]>(),
                new TerrainPathStyle[0])
        {
        }

        internal TerrainWorld(
            string name,
            IReadOnlyList<TerrainBase> bases,
            Dictionary<LayoutKey, TerrainSectionRef[]> layouts,
            int[,] distance,
            int[,] stepToward,
            Dictionary<LayoutKey, TerrainSectionRef[]> slopes,
            Dictionary<LayoutKey, TerrainSectionRef[]> plainSlopes,
            IReadOnlyList<TerrainPathStyle> pathStyles)
        {
            this.Name = name;
            this.Bases = bases;
            this.layouts = layouts;
            this.distance = distance;
            this.stepToward = stepToward;
            this.slopes = slopes;
            this.plainSlopes = plainSlopes;
            this.PathStyles = pathStyles;
        }

        public string Name { get; }

        public IReadOnlyList<TerrainBase> Bases { get; }

        /// <summary>
        /// The river and ridge styles, sorted by name.
        /// </summary>
        public IReadOnlyList<TerrainPathStyle> PathStyles { get; }

        /// <summary>
        /// True when the world has slope pieces that can stand in for cliffs.
        /// </summary>
        public bool HasSlopes => this.slopes.Count > 0;

        public TerrainPathStyle FindPathStyle(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            foreach (var style in this.PathStyles)
            {
                if (string.Equals(style.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return style;
                }
            }

            return null;
        }

        /// <summary>
        /// The slope pieces for a layout, or null. With plainOnly, leaves out the
        /// pieces made to meet a cliff (Single and Transition pieces).
        /// </summary>
        public IReadOnlyList<TerrainSectionRef> SlopeSections(int topLeft, int topRight, int bottomLeft, int bottomRight, bool plainOnly)
        {
            TerrainSectionRef[] sections;
            var source = plainOnly ? this.plainSlopes : this.slopes;
            return source.TryGetValue(new LayoutKey(topLeft, topRight, bottomLeft, bottomRight), out sections) ? sections : null;
        }

        public int Distance(int from, int to)
        {
            return this.distance[from, to];
        }

        public int StepToward(int from, int target)
        {
            return this.stepToward[from, target];
        }

        public int IndexOfNominal(int nominal)
        {
            for (var index = 0; index < this.Bases.Count; index++)
            {
                if (this.Bases[index].Nominal == nominal)
                {
                    return index;
                }
            }

            return -1;
        }

        public bool HasLayout(int topLeft, int topRight, int bottomLeft, int bottomRight)
        {
            if (topLeft == topRight && topLeft == bottomLeft && topLeft == bottomRight)
            {
                return true;
            }

            return this.layouts.ContainsKey(new LayoutKey(topLeft, topRight, bottomLeft, bottomRight));
        }

        public IReadOnlyList<TerrainSectionRef> TransitionSections(int topLeft, int topRight, int bottomLeft, int bottomRight)
        {
            TerrainSectionRef[] sections;
            if (!this.layouts.TryGetValue(new LayoutKey(topLeft, topRight, bottomLeft, bottomRight), out sections))
            {
                return null;
            }

            return sections;
        }

        public int NearestBase(int height, bool abstainOnTie)
        {
            if (this.Bases.Count == 0)
            {
                return -1;
            }

            var best = 0;
            var bestDistance = Math.Abs(height - this.Bases[0].MeasuredHeight);
            var tie = false;
            for (var index = 1; index < this.Bases.Count; index++)
            {
                var candidate = Math.Abs(height - this.Bases[index].MeasuredHeight);
                if (candidate < bestDistance)
                {
                    best = index;
                    bestDistance = candidate;
                    tie = false;
                }
                else if (candidate == bestDistance)
                {
                    tie = true;
                }
            }

            return tie && abstainOnTie ? -1 : best;
        }

        /// <summary>
        /// The base the four height samples around a corner agree on.
        /// A sample equally close to two bases does not vote. With no votes,
        /// the nearest base to the average height is used.
        /// </summary>
        public int Vote(int heightA, int heightB, int heightC, int heightD)
        {
            return this.VoteSamples(new[] { heightA, heightB, heightC, heightD });
        }

        /// <summary>
        /// The base the height samples around a corner agree on.
        /// A sample equally close to two bases does not vote. With no votes,
        /// the nearest base to the average height is used. Samples that fall
        /// off the map are left out of the list.
        /// </summary>
        public int VoteSamples(IReadOnlyList<int> heights)
        {
            if (heights == null || heights.Count == 0 || this.Bases.Count == 0)
            {
                return 0;
            }

            var votes = new int[this.Bases.Count];
            var total = 0;
            foreach (var height in heights)
            {
                this.Cast(votes, height);
                total += height;
            }

            var winner = 0;
            var best = 0;
            for (var index = 0; index < votes.Length; index++)
            {
                if (votes[index] > best)
                {
                    best = votes[index];
                    winner = index;
                }
            }

            if (best > 0)
            {
                return winner;
            }

            var fallback = this.NearestBase(total / heights.Count, false);
            return fallback < 0 ? 0 : fallback;
        }

        public List<int[]> JoinedNominalPairs()
        {
            var pairs = new List<int[]>();
            for (var left = 0; left < this.Bases.Count; left++)
            {
                for (var right = left + 1; right < this.Bases.Count; right++)
                {
                    if (this.Distance(left, right) == 1)
                    {
                        var low = this.Bases[left].Nominal;
                        var high = this.Bases[right].Nominal;
                        pairs.Add(low <= high ? new[] { low, high } : new[] { high, low });
                    }
                }
            }

            pairs.Sort((left, right) => left[0] != right[0] ? left[0].CompareTo(right[0]) : left[1].CompareTo(right[1]));
            return pairs;
        }

        private void Cast(int[] votes, int height)
        {
            var nearest = this.NearestBase(height, true);
            if (nearest >= 0)
            {
                votes[nearest]++;
            }
        }

        internal struct LayoutKey : IEquatable<LayoutKey>
        {
            public LayoutKey(int topLeft, int topRight, int bottomLeft, int bottomRight)
            {
                this.TopLeft = topLeft;
                this.TopRight = topRight;
                this.BottomLeft = bottomLeft;
                this.BottomRight = bottomRight;
            }

            public int TopLeft { get; }

            public int TopRight { get; }

            public int BottomLeft { get; }

            public int BottomRight { get; }

            public bool Equals(LayoutKey other)
            {
                return this.TopLeft == other.TopLeft
                    && this.TopRight == other.TopRight
                    && this.BottomLeft == other.BottomLeft
                    && this.BottomRight == other.BottomRight;
            }

            public override bool Equals(object obj)
            {
                return obj is LayoutKey other && this.Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = this.TopLeft;
                    hash = (hash * 397) ^ this.TopRight;
                    hash = (hash * 397) ^ this.BottomLeft;
                    hash = (hash * 397) ^ this.BottomRight;
                    return hash;
                }
            }
        }
    }
}
