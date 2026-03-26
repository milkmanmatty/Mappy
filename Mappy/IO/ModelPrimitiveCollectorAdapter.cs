namespace Mappy.IO
{
    using System.Collections.Generic;
    using Geometry;
    using TAUtil._3do;

    public class ModelPrimitiveCollectorAdapter : IModelReaderAdapter
    {
        public readonly struct CollectedFace
        {
            public CollectedFace(int colorIndex, string textureName, Vector3D[] verticesWorld)
            {
                this.ColorIndex = colorIndex;
                this.TextureName = textureName;
                this.VerticesWorld = verticesWorld;
            }

            public int ColorIndex { get; }

            public string TextureName { get; }

            public Vector3D[] VerticesWorld { get; }
        }

        private readonly List<CollectedFace> faces = new List<CollectedFace>();

        private readonly List<Vector3D> vertices = new List<Vector3D>();

        private readonly Stack<Vector3D> positions = new Stack<Vector3D>();

        public IReadOnlyList<CollectedFace> Faces => this.faces;

        public void CreateChild(string name, Vector position)
        {
            this.PushNewOffset(position);
            this.vertices.Clear();
        }

        public void BackToParent()
        {
            this.positions.Pop();
            this.vertices.Clear();
        }

        public void AddVertex(Vector v)
        {
            var basePos = this.positions.Peek();
            var vec = new Vector3D(v.X, v.Y, v.Z);
            this.vertices.Add(basePos + vec);
        }

        public void AddPrimitive(int color, string texture, int[] vertexIndices, bool isSelectionPrimitive)
        {
            if (isSelectionPrimitive || vertexIndices.Length < 3)
            {
                return;
            }

            var verts = new Vector3D[vertexIndices.Length];
            for (var i = 0; i < vertexIndices.Length; i++)
            {
                verts[i] = this.vertices[vertexIndices[i]];
            }

            var tex = string.IsNullOrEmpty(texture) ? null : texture.Trim();
            this.faces.Add(new CollectedFace(color, tex, verts));
        }

        private void PushNewOffset(Vector offset)
        {
            var offsetVec = new Vector3D(offset.X, offset.Y, offset.Z);

            Vector3D parentPosition;
            if (this.positions.Count == 0)
            {
                parentPosition = Vector3D.Zero;
            }
            else
            {
                parentPosition = this.positions.Peek();
            }

            var newPos = parentPosition + offsetVec;

            this.positions.Push(newPos);
        }
    }
}
