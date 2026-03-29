namespace Mappy.IO.Gaf
{
    using System.Collections.Generic;
    using TAUtil.Gaf;

    public class GafEntryAllFramesArrayAdapter : IGafReaderAdapter
    {
        private readonly List<GafFrame> currentFrames = new List<GafFrame>();

        private int currentEntryIndex;

        private GafEntry currentEntry;

        private GafFrame currentFrame;

        private int frameDepth;

        public GafEntry[] Entries { get; private set; }

        public void BeginRead(long entryCount)
        {
            this.Entries = new GafEntry[entryCount];
        }

        public void BeginEntry(string name, int frameCount)
        {
            this.currentEntry = new GafEntry { Name = name };
            this.currentFrames.Clear();
        }

        public void BeginFrame(int x, int y, int width, int height, int transparencyIndex, int subframeCount)
        {
            this.frameDepth++;

            if (this.frameDepth > 1)
            {
                return;
            }

            this.currentFrame = new GafFrame
            {
                OffsetX = x,
                OffsetY = y,
                Width = width,
                Height = height,
                TransparencyIndex = (byte)transparencyIndex,
            };
        }

        public void SetFrameData(byte[] data)
        {
            if (this.frameDepth > 1)
            {
                return;
            }

            this.currentFrame.Data = data;
        }

        public void EndFrame()
        {
            if (this.frameDepth > 1)
            {
                this.frameDepth--;
                return;
            }

            this.frameDepth--;
            if (this.frameDepth == 0)
            {
                this.currentFrames.Add(this.currentFrame);
            }
        }

        public void EndEntry()
        {
            this.currentEntry.Frames = this.currentFrames.ToArray();
            this.Entries[this.currentEntryIndex++] = this.currentEntry;
        }

        public void EndRead()
        {
        }
    }
}
