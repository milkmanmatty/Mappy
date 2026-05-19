namespace Mappy.UI.Controls
{
    using System;
    using System.Windows.Forms;

    // If we want to control the numeric step we need to subclass the component like it's 1999
    public class StepNumericUpDown : NumericUpDown
    {
        private const int WmMouseWheel = 0x020A;

        public int MouseWheelStep { get; set; } = 1;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmMouseWheel && this.TryHandleMouseWheel(m))
            {
                return;
            }

            base.WndProc(ref m);
        }

        private bool TryHandleMouseWheel(Message m)
        {
            if (!this.InterceptArrowKeys)
            {
                return false;
            }

            var delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
            if (delta == 0)
            {
                return false;
            }

            var step = Math.Max(1, this.MouseWheelStep);
            if (delta > 0)
            {
                this.Value = Math.Min(this.Maximum, this.Value + step);
            }
            else
            {
                this.Value = Math.Max(this.Minimum, this.Value - step);
            }

            return true;
        }
    }
}