namespace Mappy
{
    using System;
    using System.Collections.Specialized;
    using System.Drawing;
    using System.Xml.Serialization;

    public class Configuration
    {
        private const int DefaultDragAutoScrollSpeed = 16;

        private const int DefaultWheelStep = 1;

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

        public bool ShowUnitFriendlyNameOnMap { get; set; }

        public bool DoNotPromptToSaveUnsavedChanges { get; set; }

        public bool StickyClipboard { get; set; }

        public bool BlobFeatureBase { get; set; }

        public int? BlobFeatureBaseColorArgb { get; set; }

        public static Color DefaultBlobFeatureBaseColor { get; } = Color.FromArgb(255, 220, 80);

        [XmlIgnore]
        public Color BlobFeatureBaseColor
        {
            get => this.GetBlobFeatureBaseColorOrDefault();
            set => this.BlobFeatureBaseColorArgb = value.ToArgb();
        }

        public Color GetBlobFeatureBaseColorOrDefault()
        {
            return this.BlobFeatureBaseColorArgb.HasValue
                ? Color.FromArgb(this.BlobFeatureBaseColorArgb.Value)
                : DefaultBlobFeatureBaseColor;
        }

        public void GetFeatureBaseBlobFillAndBorderColors(out Color fill, out Color border)
        {
            var baseColor = this.GetBlobFeatureBaseColorOrDefault();
            fill = Color.FromArgb(160, baseColor.R, baseColor.G, baseColor.B);
            border = Color.FromArgb(
                220,
                Math.Max(0, (int)(baseColor.R * 0.7)),
                Math.Max(0, (int)(baseColor.G * 0.7)),
                Math.Max(0, (int)(baseColor.B * 0.7)));
        }

        [XmlIgnore]
        public Color GridColor
        {
            get => Color.FromArgb(this.GridColorArgb);
            set => this.GridColorArgb = value.ToArgb();
        }

        [XmlElement(ElementName = "GridColor")]
        public int GridColorArgb { get; set; }

        public StringCollection SearchPaths { get; set; }

        public RecentMapEntry[] RecentMaps { get; set; }

        public int DragAutoScrollSpeedX { get; set; } = DefaultDragAutoScrollSpeed;

        public int DragAutoScrollSpeedY { get; set; } = DefaultDragAutoScrollSpeed;

        public int? InactiveSchemaOpacityPercent { get; set; }

        public int HeightSelectedHeightWheelStep { get; set; } = DefaultWheelStep;

        public int HeightIntervalWheelStep { get; set; } = DefaultWheelStep;

        public int HeightCursorSizeWheelStep { get; set; } = DefaultWheelStep;

        public int VoidCursorSizeWheelStep { get; set; } = DefaultWheelStep;

        public int SeaLevelWheelStep { get; set; } = DefaultWheelStep;

        public bool? DefaultHeightmapVisible { get; set; }

        public bool? DefaultHeightGridVisible { get; set; }

        public bool? DefaultMinimapVisible { get; set; }

        public bool? DefaultVoidsVisible { get; set; }

        public bool? DefaultFeaturesVisible { get; set; }

        public bool? DefaultGridVisible { get; set; }

        public bool GetDefaultHeightmapVisibleOrDefault() => this.DefaultHeightmapVisible ?? false;

        public bool GetDefaultHeightGridVisibleOrDefault() => this.DefaultHeightGridVisible ?? false;

        public bool GetDefaultMinimapVisibleOrDefault() => this.DefaultMinimapVisible ?? false;

        public bool GetDefaultVoidsVisibleOrDefault() => this.DefaultVoidsVisible ?? false;

        public bool GetDefaultFeaturesVisibleOrDefault() => this.DefaultFeaturesVisible ?? true;

        public bool GetDefaultGridVisibleOrDefault() => this.DefaultGridVisible ?? false;

        public void ApplyViewDefaults(Models.CoreModel model)
        {
            model.HeightmapVisible = this.GetDefaultHeightmapVisibleOrDefault();
            model.HeightGridVisible = this.GetDefaultHeightGridVisibleOrDefault();
            model.MinimapVisible = this.GetDefaultMinimapVisibleOrDefault();
            model.VoidsVisible = this.GetDefaultVoidsVisibleOrDefault();
            model.FeaturesVisible = this.GetDefaultFeaturesVisibleOrDefault();
            model.GridVisible = this.GetDefaultGridVisibleOrDefault();
        }

        public int GetHeightSelectedHeightWheelStepOrDefault() => GetWheelStepOrDefault(this.HeightSelectedHeightWheelStep);

        public int GetHeightIntervalWheelStepOrDefault() => GetWheelStepOrDefault(this.HeightIntervalWheelStep);

        public int GetHeightCursorSizeWheelStepOrDefault() => GetWheelStepOrDefault(this.HeightCursorSizeWheelStep);

        public int GetVoidCursorSizeWheelStepOrDefault() => GetWheelStepOrDefault(this.VoidCursorSizeWheelStep);

        public int GetSeaLevelWheelStepOrDefault() => GetWheelStepOrDefault(this.SeaLevelWheelStep);

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

        private static int GetWheelStepOrDefault(int value)
        {
            return value > 0 ? value : DefaultWheelStep;
        }
    }
}
