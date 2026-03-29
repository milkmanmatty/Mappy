namespace Mappy.Data
{
    using System;

    /// <summary>
    /// Event arguments for start position change events.
    /// </summary>
    public class StartPositionChangedEventArgs : EventArgs
    {
        public StartPositionChangedEventArgs(int index, int schemaIndex = 0)
        {
            this.Index = index;
            this.SchemaIndex = schemaIndex;
        }

        public int Index { get; set; }

        public int SchemaIndex { get; set; }
    }
}