﻿namespace Mappy.Models
{
    using System.Collections.Generic;
    using System.Drawing;

    using Mappy.Collections;

    public interface IMapCommandHandler
    {
        IBindingMapModel Map { get; }

        int PlaceSection(int tileId, int x, int y);

        void TranslateSection(int index, int x, int y);

        bool TranslateFeatureBatch(IEnumerable<GridCoordinates> coords, int x, int y);

        bool TranslateFeature(Point featureCoord, int x, int y);

        bool TranslateFeature(int index, int x, int y);

        void FlushTranslation();

        bool TryPlaceFeature(string name, int x, int y);

        void RemoveSection(int index);

        void RemoveFeature(int index);

        void RemoveFeature(int x, int y);

        void RemoveFeature(Point coords);

        void SetStartPosition(int i, int x, int y);

        void TranslateStartPosition(int i, int x, int y);

        void RemoveStartPosition(int i);

        void MergeSection(int index);

        int LiftArea(int x, int y, int width, int height);
    }
}