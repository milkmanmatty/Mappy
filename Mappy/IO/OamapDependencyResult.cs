namespace Mappy.IO
{
    using System.Collections.Generic;

    public sealed class OamapDependencyResult
    {
        public List<OamapPackedFile> Files { get; } = new List<OamapPackedFile>();

        public List<string> Warnings { get; } = new List<string>();
    }
}
