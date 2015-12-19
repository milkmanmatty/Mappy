﻿namespace Mappy.Models
{
    using System;
    using System.Drawing;
    using System.Reactive.Linq;

    public class MinimapFormViewModel : IMinimapFormViewModel
    {
        private readonly CoreModel model;

        public MinimapFormViewModel(CoreModel model)
        {
            var viewportLocation = model.PropertyAsObservable(x => x.ViewportLocation, "ViewportLocation");
            var viewportWidth = model.PropertyAsObservable(x => x.ViewportWidth, "ViewportWidth");
            var viewportHeight = model.PropertyAsObservable(x => x.ViewportHeight, "ViewportHeight");

            this.MapWidth = model.PropertyAsObservable(x => x.MapWidth, "MapWidth");
            this.MapHeight = model.PropertyAsObservable(x => x.MapHeight, "MapHeight");
            this.MinimapVisible = model.PropertyAsObservable(x => x.MinimapVisible, "MinimapVisible");
            this.MinimapImage = model.PropertyAsObservable(x => x.MinimapImage, "MinimapImage");

            // set up the minimap rectangle observable
            var minimapRectWidth = this.ScaleObsWidthToMinimap(viewportWidth);
            var minimapRectHeight = this.ScaleObsHeightToMinimap(viewportHeight);
            var minimapRectSize = minimapRectWidth.CombineLatest(minimapRectHeight, (w, h) => new Size(w, h));

            var minimapRectX = this.ScaleObsWidthToMinimap(viewportLocation.Select(x => x.X));
            var minimapRectY = this.ScaleObsHeightToMinimap(viewportLocation.Select(x => x.Y));
            var minimapRectLocation = minimapRectX.CombineLatest(minimapRectY, (x, y) => new Point(x, y));

            var minimapRect = minimapRectLocation
                .CombineLatest(minimapRectSize, (l, s) => new Rectangle(l, s))
                .Replay(1);
            minimapRect.Connect();

            this.MinimapRect = minimapRect;

            this.model = model;
        }

        public IObservable<int> MapWidth { get; }

        public IObservable<int> MapHeight { get; }

        public IObservable<bool> MinimapVisible { get; }

        public IObservable<Bitmap> MinimapImage { get; }

        public IObservable<Rectangle> MinimapRect { get; }

        public void SetViewportLocation(Point location)
        {
            this.model.SetViewportLocation(location);
        }

        public void HideMinimap()
        {
            this.model.HideMinimap();
        }

        private IObservable<int> ScaleObsWidthToMinimap(IObservable<int> value)
        {
            var mapWidth = this.MapWidth.Select(x => (x * 32) - 32);
            var minimapWidth = this.MinimapImage.Select(x => x?.Width ?? 0);

            return value
                .CombineLatest(minimapWidth, (v, w) => v * w)
                .CombineLatest(mapWidth, (v, w) => v / w);
        }

        private IObservable<int> ScaleObsHeightToMinimap(IObservable<int> value)
        {
            var mapHeight = this.MapHeight.Select(x => (x * 32) - 128);
            var minimapHeight = this.MinimapImage.Select(x => x?.Height ?? 0);

            return value
                .CombineLatest(minimapHeight, (v, h) => v * h)
                .CombineLatest(mapHeight, (v, h) => v / h);
        }
    }
}