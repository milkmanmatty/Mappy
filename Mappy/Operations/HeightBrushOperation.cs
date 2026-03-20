namespace Mappy.Operations
{
    using System.Collections.Generic;
    using System.Linq;

    using Mappy.Collections;

    public class HeightBrushOperation : IReplayableOperation
    {
        private readonly IGrid<int> heightGrid;

        private readonly IReadOnlyList<HeightChange> changes;

        public HeightBrushOperation(IGrid<int> heightGrid, IEnumerable<HeightChange> changes)
        {
            this.heightGrid = heightGrid;
            this.changes = changes.ToList();
        }

        public void Execute()
        {
            foreach (var change in this.changes)
            {
                this.heightGrid[change.Index] = change.NewValue;
            }
        }

        public void Undo()
        {
            foreach (var change in this.changes)
            {
                this.heightGrid[change.Index] = change.OldValue;
            }
        }

        public HeightBrushOperation Combine(HeightBrushOperation other)
        {
            var merged = this.changes.ToDictionary(x => x.Index, x => x);
            foreach (var change in other.changes)
            {
                if (merged.TryGetValue(change.Index, out var existing))
                {
                    merged[change.Index] = new HeightChange(change.Index, existing.OldValue, change.NewValue);
                }
                else
                {
                    merged[change.Index] = change;
                }
            }

            return new HeightBrushOperation(this.heightGrid, merged.Values);
        }

        public readonly struct HeightChange
        {
            public HeightChange(int index, int oldValue, int newValue)
            {
                this.Index = index;
                this.OldValue = oldValue;
                this.NewValue = newValue;
            }

            public int Index { get; }

            public int OldValue { get; }

            public int NewValue { get; }
        }
    }
}
