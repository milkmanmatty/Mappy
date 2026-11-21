namespace Mappy.Operations
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;

    using Mappy.Collections;
    using Mappy.Models;

    /// <summary>
    /// Replaces the tiles and heights under one brush stroke.
    /// A drag combines steps so the whole stroke is a single undo.
    /// </summary>
    public sealed class TerrainBrushOperation : IReplayableOperation
    {
        private readonly List<IReplayableOperation> steps;

        private TerrainBrushOperation(List<IReplayableOperation> steps)
        {
            this.steps = steps;
        }

        public static TerrainBrushOperation Create(
            IGrid<Bitmap> tileGrid,
            IGrid<int> heightGrid,
            IReadOnlyList<TerrainBrushStamp> stamps)
        {
            return Create(tileGrid, heightGrid, stamps, null, null);
        }

        public static TerrainBrushOperation Create(
            IGrid<Bitmap> tileGrid,
            IGrid<int> heightGrid,
            IReadOnlyList<TerrainBrushStamp> stamps,
            IMapModel map,
            IEnumerable<Guid> removedFeatures)
        {
            var steps = new List<IReplayableOperation>();
            if (stamps != null)
            {
                foreach (var stamp in stamps)
                {
                    if (stamp == null)
                    {
                        continue;
                    }

                    AddCopy(steps, stamp.Tiles, tileGrid, stamp.TileX, stamp.TileY);
                    AddCopy(steps, stamp.Heights, heightGrid, stamp.TileX * 2, stamp.TileY * 2);
                }
            }

            if (map != null && removedFeatures != null)
            {
                foreach (var id in removedFeatures)
                {
                    steps.Add(new RemoveFeatureOperation(map, id));
                }
            }

            if (steps.Count == 0)
            {
                return null;
            }

            return new TerrainBrushOperation(steps);
        }

        public void Execute()
        {
            foreach (var step in this.steps)
            {
                step.Execute();
            }
        }

        public void Undo()
        {
            for (var index = this.steps.Count - 1; index >= 0; index--)
            {
                this.steps[index].Undo();
            }
        }

        public TerrainBrushOperation Combine(TerrainBrushOperation other)
        {
            if (other == null)
            {
                return this;
            }

            var combined = new List<IReplayableOperation>(this.steps.Count + other.steps.Count);
            combined.AddRange(this.steps);
            combined.AddRange(other.steps);
            return new TerrainBrushOperation(combined);
        }

        /// <summary>
        /// Adds an already executed step, such as the minimap refresh at the end of a stroke.
        /// </summary>
        public TerrainBrushOperation Append(IReplayableOperation step)
        {
            if (step == null)
            {
                return this;
            }

            var combined = new List<IReplayableOperation>(this.steps.Count + 1);
            combined.AddRange(this.steps);
            combined.Add(step);
            return new TerrainBrushOperation(combined);
        }

        private static void AddCopy<T>(List<IReplayableOperation> steps, IGrid<T> source, IGrid<T> destination, int destX, int destY)
        {
            if (source == null || destination == null || source.Width <= 0 || source.Height <= 0)
            {
                return;
            }

            var sourceX = 0;
            var sourceY = 0;
            var width = source.Width;
            var height = source.Height;
            if (destX < 0)
            {
                sourceX = -destX;
                width += destX;
                destX = 0;
            }

            if (destY < 0)
            {
                sourceY = -destY;
                height += destY;
                destY = 0;
            }

            if (destX >= destination.Width || destY >= destination.Height)
            {
                return;
            }

            width = Math.Min(width, destination.Width - destX);
            height = Math.Min(height, destination.Height - destY);
            width = Math.Min(width, source.Width - sourceX);
            height = Math.Min(height, source.Height - sourceY);
            if (width <= 0 || height <= 0)
            {
                return;
            }

            steps.Add(new CopyAreaOperation<T>(source, destination, sourceX, sourceY, destX, destY, width, height));
        }
    }
}
