namespace Mappy.Operations
{
    using System.Collections.Generic;
    using System.Linq;

    using Mappy.Collections;

    public sealed class VoidBrushOperation : IReplayableOperation
    {
        private readonly IGrid<bool> grid;

        private readonly IReadOnlyList<VoidChange> changes;

        public VoidBrushOperation(IGrid<bool> grid, IEnumerable<VoidChange> changes)
        {
            this.grid = grid;
            this.changes = changes.ToList();
        }

        public void Execute()
        {
            foreach (var change in this.changes)
            {
                this.grid[change.Index] = change.NewValue;
            }
        }

        public void Undo()
        {
            foreach (var change in this.changes)
            {
                this.grid[change.Index] = change.OldValue;
            }
        }

        public VoidBrushOperation Combine(VoidBrushOperation other)
        {
            var map = this.changes.ToDictionary(x => x.Index, x => x);

            foreach (var change in other.changes)
            {
                VoidChange previous;
                if (map.TryGetValue(change.Index, out previous))
                {
                    map[change.Index] = new VoidChange(change.Index, previous.OldValue, change.NewValue);
                }
                else
                {
                    map[change.Index] = change;
                }
            }

            return new VoidBrushOperation(this.grid, map.Values);
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
