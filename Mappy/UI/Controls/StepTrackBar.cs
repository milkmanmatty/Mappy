namespace Mappy.UI.Controls
{
    using System;
    using System.Windows.Forms;

    // If we want to control the numeric step we need to subclass the component like it's 1999
    public class StepTrackBar : TrackBar
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
            var delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
            if (delta == 0)
            {
                return false;
            }

            var step = Math.Max(1, this.MouseWheelStep);
            var newValue = this.Value + (delta > 0 ? step : -step);
            newValue = Math.Max(this.Minimum, Math.Min(this.Maximum, newValue));
            this.Value = newValue;
            return true;
        }
    }
}