namespace Mappy.Operations
{
    using System.Collections.Generic;

    using Mappy.Collections;
    using Mappy.Data;
    using Mappy.Models;

    public class BatchMoveSchemaUnitsOperation : IReplayableOperation
    {
        private readonly IMapModel map;

        private readonly HashSet<MapUnitRef> refs;

        private readonly int dx;

        private readonly int dy;

        public BatchMoveSchemaUnitsOperation(IMapModel map, IEnumerable<MapUnitRef> unitRefs, int dx, int dy)
        {
            this.map = map;
            this.refs = new HashSet<MapUnitRef>(unitRefs);
            this.dx = dx;
            this.dy = dy;
        }

        public void Execute()
        {
            foreach (var r in this.refs)
            {
                this.ApplyDelta(r, this.dx, this.dy);
            }
        }

        public void Undo()
        {
            foreach (var r in this.refs)
            {
                this.ApplyDelta(r, -this.dx, -this.dy);
            }
        }

        public bool CanCombine(BatchMoveSchemaUnitsOperation other)
        {
            return this.refs.SetEquals(other.refs);
        }

        public BatchMoveSchemaUnitsOperation Combine(BatchMoveSchemaUnitsOperation other)
        {
            return new BatchMoveSchemaUnitsOperation(
                this.map,
                this.refs,
                this.dx + other.dx,
                this.dy + other.dy);
        }

        private void ApplyDelta(MapUnitRef r, int adx, int ady)
        {
            var u = this.map.Attributes.GetUnit(r.SchemaIndex, r.UnitId);
            var nu = u.ClonePreservingId();
            nu.XPos += adx;
            nu.ZPos += ady;
            var hx = nu.XPos / 16;
            var hz = nu.ZPos / 16;
            var grid = this.map.Tile.HeightGrid;
            if (hx >= 0 && hz >= 0 && hx < grid.Width && hz < grid.Height)
            {
                nu.YPos = grid.Get(hx, hz);
            }

            this.map.UpdateSchemaUnit(r.SchemaIndex, nu);
        }
    }
}
