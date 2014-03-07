﻿namespace Mappy.Presentation
{
    using System.Drawing;
    using System.Windows.Forms;

    using Mappy.Models.Session;
    using Mappy.UI.Controls;

    public class MapCommandHandler : IMapCommandHandler
    {
        private readonly ISelectionCommandHandler handler;

        private bool mouseDown;

        private Point lastMousePos;

        private bool bandboxMode;

        public MapCommandHandler(ISelectionCommandHandler handler)
        {
            this.handler = handler;
        }

        public void DragDrop(IDataObject data, int virtualX, int virtualY)
        {
            if (data.GetDataPresent(typeof(StartPositionDragData)))
            {
                StartPositionDragData posData = (StartPositionDragData)data.GetData(typeof(StartPositionDragData));
                this.handler.DragDropStartPosition(posData.PositionNumber, virtualX, virtualY);
            }
            else
            {
                string dataString = data.GetData(DataFormats.Text).ToString();
                int id;
                if (int.TryParse(dataString, out id))
                {
                    this.handler.DragDropTile(id, virtualX, virtualY);
                }
                else
                {
                    this.handler.DragDropFeature(dataString, virtualX, virtualY);
                }
            }
        }

        public void MouseDown(int virtualX, int virtualY)
        {
            this.mouseDown = true;
            this.lastMousePos = new Point(virtualX, virtualY);

            if (!this.handler.IsInSelection(virtualX, virtualY))
            {
                if (!this.handler.SelectAtPoint(virtualX, virtualY))
                {
                    this.handler.StartBandbox(virtualX, virtualY);
                    this.bandboxMode = true;
                }
            }
        }

        public void MouseMove(int virtualX, int virtualY)
        {
            try
            {
                if (!this.mouseDown)
                {
                    return;
                }

                if (this.bandboxMode)
                {
                    this.handler.GrowBandbox(
                        virtualX - this.lastMousePos.X,
                        virtualY - this.lastMousePos.Y);
                }
                else
                {
                    this.handler.TranslateSelection(
                        virtualX - this.lastMousePos.X,
                        virtualY - this.lastMousePos.Y);
                }
            }
            finally
            {
                this.lastMousePos = new Point(virtualX, virtualY);
            }
        }

        public void MouseUp(int virtualX, int virtualY)
        {
            if (this.bandboxMode)
            {
                this.handler.CommitBandbox();
                this.bandboxMode = false;
            }
            else
            {
                this.handler.FlushTranslation();
            }

            this.mouseDown = false;
        }

        public void KeyDown(Keys key)
        {
            if (key == Keys.Delete)
            {
                this.handler.DeleteSelection();
            }
        }

        public void LostFocus()
        {
            this.handler.ClearSelection();
        }
    }
}
