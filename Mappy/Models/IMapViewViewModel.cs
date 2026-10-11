namespace Mappy.Models
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    using Mappy.UI.Controls;

    public interface IMapViewViewModel
    {
        IObservable<Size> CanvasSize { get; }

        IObservable<ILayer> ItemsLayer { get; }

        IObservable<ILayer> VoidLayer { get; }

        ILayer GuidesLayer { get; }

        ILayer GridLayer { get; }

        ILayer SelectionOutlineLayer { get; }

        ILayer MissionPathLayer { get; }

        ILayer ResourceMarkerLayer { get; }

        IObservable<Point> ViewportLocation { get; }

        IObservable<bool> HeightEditMode { get; }

        IObservable<bool> VoidEditMode { get; }

        IObservable<bool> MarkerEditMode { get; }

        IObservable<bool> MissionPickPrecisionCursor { get; }

        void MouseLeftDown(Point location);

        void MouseRightDown(Point location, Point screenLocation);

        void MouseDoubleClick(Point location);

        void MouseMove(Point locattion);

        void MouseUp();

        void KeyDown(Keys key);

        void SetMissionPathsVisible(bool visible);

        void SetMarkerDrawFreehand(bool freehand);

        bool ShiftMouseWheel(int delta, bool ctrlPressed);

        void LeaveFocus();

        void DragDrop(IDataObject item, Point location, Point screenLocation);

        void UpdateDragPreview(IDataObject item, Point location);

        void ClearDragPreview();

        void ClientSizeChanged(Size size);

        void ScrollPositionChanged(Point position);
    }
}
