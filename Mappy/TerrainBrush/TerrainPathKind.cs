namespace Mappy.TerrainBrush
{
    /// <summary>
    /// How a path piece marks the edges it runs through: a river cuts a
    /// channel below the ground, a ridge raises a crest above it.
    /// </summary>
    public enum TerrainPathKind
    {
        Channel,
        Crest,
    }
}
