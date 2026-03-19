namespace Mappy.Operations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Collections;
    using Models;

    public sealed class VoidBrushOperation : IReplayableOperation
    {
        private readonly IGrid<bool> grid;

        private readonly IMapModel map;

        private readonly IReadOnlyList<VoidChange> changes;

        private readonly IReadOnlyList<FeatureInstance> removedFeatures;

        public VoidBrushOperation(
            IGrid<bool> grid,
            IEnumerable<VoidChange> changes,
            IMapModel map,
            IEnumerable<FeatureInstance> removedFeatures)
        {
            this.grid = grid;
            this.changes = changes.ToList();
            this.map = map;
            this.removedFeatures = removedFeatures.ToList();
        }

        public void Execute()
        {
            foreach (var change in this.changes)
            {
                this.grid[change.Index] = change.NewValue;
            }

            foreach (var feature in this.removedFeatures)
            {
                if (this.map.HasFeatureInstanceAt(feature.X, feature.Y))
                {
                    this.map.RemoveFeatureInstance(feature.Id);
                }
            }
        }

        public void Undo()
        {
            foreach (var change in this.changes)
            {
                this.grid[change.Index] = change.OldValue;
            }

            foreach (var feature in this.removedFeatures)
            {
                if (!this.map.HasFeatureInstanceAt(feature.X, feature.Y))
                {
                    this.map.AddFeatureInstance(feature);
                }
            }
        }

        public VoidBrushOperation Combine(VoidBrushOperation other)
        {
            if (!ReferenceEquals(this.map, other.map))
            {
                throw new InvalidOperationException("Cannot combine void operations from different maps.");
            }

            var mappings = this.changes.ToDictionary(x => x.Index, x => x);

            foreach (var change in other.changes)
            {
                VoidChange previous;
                if (mappings.TryGetValue(change.Index, out previous))
                {
                    mappings[change.Index] = new VoidChange(change.Index, previous.OldValue, change.NewValue);
                }
                else
                {
                    mappings[change.Index] = change;
                }
            }

            var removed = this.removedFeatures
                .Concat(other.removedFeatures)
                .GroupBy(x => x.Id)
                .Select(x => x.First());

            return new VoidBrushOperation(this.grid, mappings.Values, this.map, removed);
        }

        public readonly struct VoidChange
        {
            public VoidChange(int index, bool oldValue, bool newValue)
            {
                this.Index = index;
                this.OldValue = oldValue;
                this.NewValue = newValue;
            }

            public int Index { get; }

            public bool OldValue { get; }

            public bool NewValue { get; }
        }
    }
}
