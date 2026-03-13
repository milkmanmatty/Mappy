namespace Mappy
{
    using System.Collections.Specialized;
    using System.Drawing;
    using System.Xml.Serialization;

    public class Configuration
    {
        private const int DefaultDragAutoScrollSpeed = 16;

        [XmlIgnore]
        public Color GridColor
        {
            get => Color.FromArgb(this.GridColorArgb);
            set => this.GridColorArgb = value.ToArgb();
        }

        [XmlElement(ElementName = "GridColor")]
        public int GridColorArgb { get; set; }

        public StringCollection SearchPaths { get; set; }

        public int DragAutoScrollSpeedX { get; set; } = DefaultDragAutoScrollSpeed;

        public int DragAutoScrollSpeedY { get; set; } = DefaultDragAutoScrollSpeed;

        public int GetDragAutoScrollSpeedXOrDefault()
        {
            return this.DragAutoScrollSpeedX > 0 ? this.DragAutoScrollSpeedX : DefaultDragAutoScrollSpeed;
        }

        public int GetDragAutoScrollSpeedYOrDefault()
        {
            return this.DragAutoScrollSpeedY > 0 ? this.DragAutoScrollSpeedY : DefaultDragAutoScrollSpeed;
        }
    }
}
