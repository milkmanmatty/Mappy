namespace Mappy
{
    using System;
    using System.Collections.Specialized;
    using System.Drawing;
    using System.Xml.Serialization;

    public class Configuration
    {
        private const int DefaultDragAutoScrollSpeed = 16;

        /// <summary>
        /// Gets or sets the main window state: 0 = Normal, 1 = Minimized, 2 = Maximized.
        /// </summary>
        public int WindowState { get; set; }

        public int WindowLocationX { get; set; }

        public int WindowLocationY { get; set; }

        public int WindowSizeWidth { get; set; }

        public int WindowSizeHeight { get; set; }

        public int SidebarTabsWidth { get; set; }

        public bool FullResourceNames { get; set; }

        public bool ShowFeatureReclaimAmounts { get; set; }

        public bool ShowCalculatedMetalDepositValue { get; set; }

        public bool ShowUnitFriendlyNameFirst { get; set; }

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

        public int? InactiveSchemaOpacityPercent { get; set; }

        public int GetInactiveSchemaOpacityPercentForDialog()
        {
            var p = this.InactiveSchemaOpacityPercent ?? 38;
            return Math.Max(0, Math.Min(100, p));
        }

        public float GetInactiveSchemaOpacityOrDefault()
        {
            var p = this.InactiveSchemaOpacityPercent ?? 38;
            p = Math.Max(0, Math.Min(100, p));
            return p / 100f;
        }

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
