namespace Mappy.UI.Controls
{
    using System.Drawing;

    public sealed class SelectionOutlineLayer : AbstractLayer
    {
        public SelectableItemsLayer Source { get; set; }

        protected override void DoDraw(Graphics graphics, Rectangle clipRectangle)
        {
            this.Source?.DrawSelectionOutlines(graphics, clipRectangle);
        }
    }
}
